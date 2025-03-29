using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;
using MaintainEase.DbMigrator.Plugins.MigrationPlugins.Helpers;
using MaintainEase.DbMigrator.Plugins.MigrationPlugins.Models;

namespace MaintainEase.DbMigrator.Plugins.MigrationPlugins.Handlers
{
    /// <summary>
    /// Migration handler for SQL Server
    /// </summary>
    public class SqlServerMigrationHandler : IMigrationHandler
    {
        private readonly ILogger<SqlServerMigrationHandler> _logger;
        private readonly DatabaseUtilities _databaseUtils;
        private readonly ProjectAnalyzer _projectAnalyzer;
        private readonly EfCoreCommandExecutor _commandExecutor;
        private readonly ProxyProjectGenerator _proxyGenerator;

        public SqlServerMigrationHandler(
            ILogger<SqlServerMigrationHandler> logger = null,
            DatabaseUtilities databaseUtils = null,
            ProjectAnalyzer projectAnalyzer = null,
            EfCoreCommandExecutor commandExecutor = null,
            ProxyProjectGenerator proxyGenerator = null)
        {
            _logger = logger;
            _databaseUtils = databaseUtils ?? new DatabaseUtilities(logger);
            _projectAnalyzer = projectAnalyzer ?? new ProjectAnalyzer(logger);
            _commandExecutor = commandExecutor ?? new EfCoreCommandExecutor(logger);
            _proxyGenerator = proxyGenerator ?? new ProxyProjectGenerator(logger);
        }

        /// <summary>
        /// Gets the provider type this handler supports
        /// </summary>
        public string ProviderType => "SqlServer";

        /// <summary>
        /// Creates a new database migration using EF Core's tools
        /// </summary>
        public async Task<MigrationResult> CreateMigrationAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Creating SQL Server migration: {MigrationName}", request.MigrationName);

                // Find solution directory
                string solutionDir = _projectAnalyzer.FindSolutionDirectory(Directory.GetCurrentDirectory());
                if (string.IsNullOrEmpty(solutionDir))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find solution directory. Make sure you're running from within the solution."
                    };
                }

                // Determine proper paths
                string infrastructureProjectPath = Path.Combine(solutionDir, "src", "MaintainEase.Infrastructure");
                if (!Directory.Exists(infrastructureProjectPath))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = $"Infrastructure project not found at: {infrastructureProjectPath}"
                    };
                }

                // When building tenantPath, make it correctly target Infrastructure project:
                string baseMigrationsFolder = Path.Combine(infrastructureProjectPath, "Migrations");
                if (!Directory.Exists(baseMigrationsFolder))
                {
                    Directory.CreateDirectory(baseMigrationsFolder);
                }

                string tenantPath = Path.Combine(baseMigrationsFolder,
                    ProviderType, // This class's ProviderType property is "SqlServer"
                    request.TenantId ?? "default");

                if (!Directory.Exists(tenantPath))
                {
                    Directory.CreateDirectory(tenantPath);
                    _logger?.LogInformation("Created migrations directory: {OutputDir}", tenantPath);
                }

                // Create a migration proxy project
                string tempProxyDir = Path.Combine(Path.GetTempPath(), $"MigrationProxy_{Guid.NewGuid().ToString("N")}");
                Directory.CreateDirectory(tempProxyDir);
                _logger?.LogInformation("Created temporary proxy directory: {TempDir}", tempProxyDir);

                try
                {
                    // First, analyze the Infrastructure project to find DbContext types
                    var dbContextTypes = await _projectAnalyzer.AnalyzeDbContextsInProject(infrastructureProjectPath);
                    if (dbContextTypes.Count == 0)
                    {
                        return new MigrationResult
                        {
                            Success = false,
                            ErrorMessage = "No DbContext classes found in the Infrastructure project."
                        };
                    }

                    // Determine DbContext to use
                    string contextName = request.AdditionalInfo != null &&
                                        request.AdditionalInfo.TryGetValue("DbContext", out var dbContext)
                                        ? dbContext
                                        : "AppDbContext";

                    // Match the requested context with found contexts (case-insensitive)
                    var matchingContext = dbContextTypes.FirstOrDefault(c =>
                        string.Equals(c.Name, contextName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.Name, contextName + "DbContext", StringComparison.OrdinalIgnoreCase));

                    if (matchingContext == null)
                    {
                        // If no match by name, see if we have an AppDbContext or just use the first one
                        matchingContext = dbContextTypes.FirstOrDefault(c => c.Name.Equals("AppDbContext", StringComparison.OrdinalIgnoreCase))
                                      ?? dbContextTypes.First();

                        _logger?.LogWarning("Requested context '{RequestedContext}' not found. Using '{FoundContext}' instead.",
                            contextName, matchingContext.Name);

                        // Update context name to use in the command
                        contextName = matchingContext.FullName;
                    }
                    else
                    {
                        // Use the full name with namespace
                        contextName = matchingContext.FullName;
                    }

                    _logger?.LogInformation("Using DbContext: {ContextName}", contextName);

                    // Create a minimal proxy project that references Infrastructure
                    await _proxyGenerator.CreateProxyProjectAsync(tempProxyDir, infrastructureProjectPath, dbContextTypes);

                    // Create a temporary script to run EF Core commands
                    string scriptPath = Path.Combine(Path.GetTempPath(), $"ef_migration_{Guid.NewGuid()}.cmd");
                    string migrationId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                    // Build the EF Core migrations command using our proxy project
                    string efCommand = $"ef migrations add {request.MigrationName} " +
                                     $"--context {contextName} " +
                                     $"--project \"{tempProxyDir}\" " +
                                     $"--output-dir \"{tenantPath}\" " +
                                     $"--verbose"; // Use verbose flag for better diagnostics

                    await File.WriteAllTextAsync(scriptPath, $"@echo off\ncd \"{tempProxyDir}\"\ndotnet {efCommand}", cancellationToken);

                    _logger?.LogInformation("EF Core command: dotnet {Command}", efCommand);

                    // Execute the command
                    var result = await _commandExecutor.ExecuteEfCoreCommandAsync(scriptPath, cancellationToken);

                    // Clean up the temporary script
                    try { File.Delete(scriptPath); } catch { /* Ignore errors */ }

                    if (!result.Success)
                    {
                        return result;
                    }

                    // Verify the migration files were created
                    var migrationFiles = await _commandExecutor.FindMigrationFilesAsync(tenantPath, request.MigrationName);

                    if (migrationFiles.Count > 0)
                    {
                        _logger?.LogInformation("Found {Count} migration files in {Path}", migrationFiles.Count, tenantPath);

                        result.AppliedMigrations = migrationFiles.Select(file => new MigrationInfo
                        {
                            Id = migrationId,
                            Name = request.MigrationName,
                            Created = DateTime.UtcNow,
                            Script = file
                        }).ToList();

                        return result;
                    }

                    // Fallback if no files found
                    _logger?.LogWarning("EF Core reported success but no migration files were found.");

                    // If EF Core reported success but we can't find the files, create basic migration files
                    _logger?.LogWarning("Creating basic migration files as fallback.");
                    return await _commandExecutor.CreateBasicMigrationAsync(request, tenantPath, migrationId, cancellationToken);
                }
                finally
                {
                    // Clean up the temporary proxy directory
                    try
                    {
                        Directory.Delete(tempProxyDir, true);
                        _logger?.LogInformation("Cleaned up temporary proxy directory");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to clean up temporary proxy directory: {Error}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating SQL Server migration: {MigrationName}", request.MigrationName);

                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Applies pending migrations
        /// </summary>
        public async Task<MigrationResult> MigrateAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Applying migrations using connection string: {ConnectionString}",
                    _databaseUtils.MaskConnectionString(request.ConnectionConfig.ConnectionString));

                // Try to find dotnet executable
                string dotnetPath = _projectAnalyzer.FindDotnetExecutable();
                if (string.IsNullOrEmpty(dotnetPath))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find dotnet executable. Please ensure .NET SDK is installed."
                    };
                }

                // Find solution directory
                string solutionDir = _projectAnalyzer.FindSolutionDirectory(Directory.GetCurrentDirectory());
                if (string.IsNullOrEmpty(solutionDir))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find solution directory."
                    };
                }

                // Find Infrastructure project path
                string infrastructureProjectPath = Path.Combine(solutionDir, "src", "MaintainEase.Infrastructure");
                if (!Directory.Exists(infrastructureProjectPath))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find Infrastructure project."
                    };
                }

                // Analyze the Infrastructure project to find DbContext types
                var dbContextTypes = await _projectAnalyzer.AnalyzeDbContextsInProject(infrastructureProjectPath);
                if (dbContextTypes.Count == 0)
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "No DbContext classes found in the Infrastructure project."
                    };
                }

                // Determine DbContext to use
                string contextName = request.AdditionalInfo != null &&
                                    request.AdditionalInfo.TryGetValue("DbContext", out var dbContext)
                                    ? dbContext
                                    : "AppDbContext";

                // Match the requested context with found contexts (case-insensitive)
                var matchingContext = dbContextTypes.FirstOrDefault(c =>
                    string.Equals(c.Name, contextName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Name, contextName + "DbContext", StringComparison.OrdinalIgnoreCase));

                if (matchingContext == null)
                {
                    // If no match by name, see if we have an AppDbContext or just use the first one
                    matchingContext = dbContextTypes.FirstOrDefault(c => c.Name.Equals("AppDbContext", StringComparison.OrdinalIgnoreCase))
                                   ?? dbContextTypes.First();

                    _logger?.LogWarning("Requested context '{RequestedContext}' not found. Using '{FoundContext}' instead.",
                        contextName, matchingContext.Name);

                    // Update context name to use in the command
                    contextName = matchingContext.FullName;
                }
                else
                {
                    // Use the full name with namespace
                    contextName = matchingContext.FullName;
                }

                // If creating a backup was requested
                string backupPath = null;
                if (request.CreateBackup)
                {
                    backupPath = await _databaseUtils.CreateBackupAsync(request, cancellationToken);
                    _logger?.LogInformation("Created SQL Server backup at: {BackupPath}", backupPath);
                }

                // Create a migration proxy project
                string tempProxyDir = Path.Combine(Path.GetTempPath(), $"MigrationProxy_{Guid.NewGuid().ToString("N")}");
                Directory.CreateDirectory(tempProxyDir);

                try
                {
                    // Create a minimal proxy project that references Infrastructure
                    await _proxyGenerator.CreateProxyProjectAsync(tempProxyDir, infrastructureProjectPath, dbContextTypes);

                    // Create a temporary script to run EF Core commands
                    string scriptPath = Path.Combine(Path.GetTempPath(), $"ef_migration_{Guid.NewGuid()}.cmd");

                    // Build the database update command
                    string efCommand = $"ef database update " +
                                     $"--context {contextName} " +
                                     $"--project \"{tempProxyDir}\" " +
                                     $"--verbose";

                    // Write connection string to environment variable for security
                    string scriptContent = $@"@echo off
cd ""{tempProxyDir}""
set ConnectionStrings__DefaultConnection={request.ConnectionConfig.ConnectionString}
dotnet {efCommand}";

                    await File.WriteAllTextAsync(scriptPath, scriptContent, cancellationToken);

                    _logger?.LogInformation("EF Core command: dotnet {Command}", efCommand);

                    var result = await _commandExecutor.ExecuteEfCoreCommandAsync(scriptPath, cancellationToken);

                    // Clean up the temporary script
                    try { File.Delete(scriptPath); } catch { /* Ignore errors */ }

                    if (!result.Success)
                    {
                        return result;
                    }

                    // Get the applied migrations
                    result.BackupPath = backupPath;
                    var migrations = await GetStatusAsync(request, cancellationToken);
                    result.AppliedMigrations = migrations.AppliedMigrations;

                    return result;
                }
                finally
                {
                    // Clean up the temporary proxy directory
                    try
                    {
                        Directory.Delete(tempProxyDir, true);
                        _logger?.LogInformation("Cleaned up temporary proxy directory");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to clean up temporary proxy directory: {Error}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying migrations");

                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets migration status
        /// </summary>
        public async Task<MigrationStatus> GetStatusAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Getting migration status using connection string: {ConnectionString}",
                    _databaseUtils.MaskConnectionString(request.ConnectionConfig.ConnectionString));

                // Find solution directory
                string solutionDir = _projectAnalyzer.FindSolutionDirectory(Directory.GetCurrentDirectory());
                if (string.IsNullOrEmpty(solutionDir))
                {
                    return new MigrationStatus
                    {
                        ErrorMessage = "Could not find solution directory."
                    };
                }

                // Find Infrastructure project path
                string infrastructureProjectPath = Path.Combine(solutionDir, "src", "MaintainEase.Infrastructure");
                if (!Directory.Exists(infrastructureProjectPath))
                {
                    return new MigrationStatus
                    {
                        ErrorMessage = "Could not find Infrastructure project."
                    };
                }

                // Analyze the Infrastructure project to find DbContext types
                var dbContextTypes = await _projectAnalyzer.AnalyzeDbContextsInProject(infrastructureProjectPath);
                if (dbContextTypes.Count == 0)
                {
                    return new MigrationStatus
                    {
                        ErrorMessage = "No DbContext classes found in the Infrastructure project."
                    };
                }

                // Determine DbContext to use
                string contextName = request.AdditionalInfo != null &&
                                    request.AdditionalInfo.TryGetValue("DbContext", out var dbContext)
                                    ? dbContext
                                    : "AppDbContext";

                // Match the requested context with found contexts (case-insensitive)
                var matchingContext = dbContextTypes.FirstOrDefault(c =>
                    string.Equals(c.Name, contextName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Name, contextName + "DbContext", StringComparison.OrdinalIgnoreCase));

                if (matchingContext == null)
                {
                    // If no match by name, see if we have an AppDbContext or just use the first one
                    matchingContext = dbContextTypes.FirstOrDefault(c => c.Name.Equals("AppDbContext", StringComparison.OrdinalIgnoreCase))
                                   ?? dbContextTypes.First();

                    _logger?.LogWarning("Requested context '{RequestedContext}' not found. Using '{FoundContext}' instead.",
                        contextName, matchingContext.Name);

                    // Update context name to use in the command
                    contextName = matchingContext.FullName;
                }
                else
                {
                    // Use the full name with namespace
                    contextName = matchingContext.FullName;
                }

                // Create a migration proxy project
                string tempProxyDir = Path.Combine(Path.GetTempPath(), $"MigrationProxy_{Guid.NewGuid().ToString("N")}");
                Directory.CreateDirectory(tempProxyDir);

                try
                {
                    // Create a minimal proxy project that references Infrastructure
                    await _proxyGenerator.CreateProxyProjectAsync(tempProxyDir, infrastructureProjectPath, dbContextTypes);

                    // Create a temporary script to run EF Core commands
                    string scriptPath = Path.Combine(Path.GetTempPath(), $"ef_migration_{Guid.NewGuid()}.cmd");

                    // Build the list command
                    string efCommand = $"ef migrations list " +
                                     $"--context {contextName} " +
                                     $"--project \"{tempProxyDir}\" " +
                                     $"--json";

                    // Write connection string to environment variable for security
                    string scriptContent = $@"@echo off
cd ""{tempProxyDir}""
set ConnectionStrings__DefaultConnection={request.ConnectionConfig.ConnectionString}
dotnet {efCommand}";

                    await File.WriteAllTextAsync(scriptPath, scriptContent, cancellationToken);

                    // Execute the command
                    var status = new MigrationStatus
                    {
                        ProviderName = ProviderType,
                        DatabaseName = _databaseUtils.ExtractDatabaseName(request.ConnectionConfig.ConnectionString),
                        DatabaseVersion = "SQL Server"
                    };

                    // Use our command executor to run the command and parse the output
                    await _commandExecutor.ParseMigrationsOutput(scriptPath, status, cancellationToken);

                    return status;
                }
                finally
                {
                    // Clean up the temporary proxy directory
                    try
                    {
                        Directory.Delete(tempProxyDir, true);
                        _logger?.LogInformation("Cleaned up temporary proxy directory");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to clean up temporary proxy directory: {Error}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting migration status");

                return new MigrationStatus
                {
                    HasPendingMigrations = false,
                    PendingMigrationsCount = 0,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Generates scripts for pending migrations without applying them
        /// </summary>
        public async Task<MigrationResult> GenerateScriptsAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Generating migration scripts for connection string: {ConnectionString}",
                    _databaseUtils.MaskConnectionString(request.ConnectionConfig.ConnectionString));

                // Create output directory if it doesn't exist
                var outputDir = string.IsNullOrEmpty(request.OutputDirectory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "Scripts")
                    : request.OutputDirectory;

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Find solution directory
                string solutionDir = _projectAnalyzer.FindSolutionDirectory(Directory.GetCurrentDirectory());
                if (string.IsNullOrEmpty(solutionDir))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find solution directory."
                    };
                }

                // Find Infrastructure project path
                string infrastructureProjectPath = Path.Combine(solutionDir, "src", "MaintainEase.Infrastructure");
                if (!Directory.Exists(infrastructureProjectPath))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find Infrastructure project."
                    };
                }

                // Analyze the Infrastructure project to find DbContext types
                var dbContextTypes = await _projectAnalyzer.AnalyzeDbContextsInProject(infrastructureProjectPath);
                if (dbContextTypes.Count == 0)
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "No DbContext classes found in the Infrastructure project."
                    };
                }

                // Determine DbContext to use
                string contextName = request.AdditionalInfo != null &&
                                    request.AdditionalInfo.TryGetValue("DbContext", out var dbContext)
                                    ? dbContext
                                    : "AppDbContext";

                // Match the requested context with found contexts (case-insensitive)
                var matchingContext = dbContextTypes.FirstOrDefault(c =>
                    string.Equals(c.Name, contextName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Name, contextName + "DbContext", StringComparison.OrdinalIgnoreCase));

                if (matchingContext == null)
                {
                    // If no match by name, see if we have an AppDbContext or just use the first one
                    matchingContext = dbContextTypes.FirstOrDefault(c => c.Name.Equals("AppDbContext", StringComparison.OrdinalIgnoreCase))
                                   ?? dbContextTypes.First();

                    _logger?.LogWarning("Requested context '{RequestedContext}' not found. Using '{FoundContext}' instead.",
                        contextName, matchingContext.Name);

                    // Update context name to use in the command
                    contextName = matchingContext.FullName;
                }
                else
                {
                    // Use the full name with namespace
                    contextName = matchingContext.FullName;
                }

                // Create a migration proxy project
                string tempProxyDir = Path.Combine(Path.GetTempPath(), $"MigrationProxy_{Guid.NewGuid().ToString("N")}");
                Directory.CreateDirectory(tempProxyDir);
                string outputScriptPath = Path.Combine(outputDir, $"migration_script_{DateTime.Now:yyyyMMddHHmmss}.sql");

                try
                {
                    // Create a minimal proxy project that references Infrastructure
                    await _proxyGenerator.CreateProxyProjectAsync(tempProxyDir, infrastructureProjectPath, dbContextTypes);

                    // Create a temporary script to run EF Core commands
                    string scriptPath = Path.Combine(Path.GetTempPath(), $"ef_migration_{Guid.NewGuid()}.cmd");

                    // Build the script command
                    string efCommand = $"ef migrations script " +
                                     $"--context {contextName} " +
                                     $"--project \"{tempProxyDir}\" " +
                                     $"--output \"{outputScriptPath}\"";

                    // Write connection string to environment variable for security
                    string scriptContent = $@"@echo off
cd ""{tempProxyDir}""
set ConnectionStrings__DefaultConnection={request.ConnectionConfig.ConnectionString}
dotnet {efCommand}";

                    await File.WriteAllTextAsync(scriptPath, scriptContent, cancellationToken);

                    var result = await _commandExecutor.ExecuteEfCoreCommandAsync(scriptPath, cancellationToken);

                    // Clean up the temporary script
                    try { File.Delete(scriptPath); } catch { /* Ignore errors */ }

                    if (!result.Success)
                    {
                        return result;
                    }

                    // Check if the script file was created
                    if (File.Exists(outputScriptPath))
                    {
                        result.ScriptsPath = outputDir;
                        result.AppliedMigrations = new List<MigrationInfo>
                        {
                            new MigrationInfo
                            {
                                Script = outputScriptPath,
                                Name = "Generated Migration Script"
                            }
                        };
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = "Script file was not created.";
                    }

                    return result;
                }
                finally
                {
                    // Clean up the temporary proxy directory
                    try
                    {
                        Directory.Delete(tempProxyDir, true);
                        _logger?.LogInformation("Cleaned up temporary proxy directory");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to clean up temporary proxy directory: {Error}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating migration scripts");

                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        public async Task<bool> TestConnectionAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            // Delegate to database utilities
            return await _databaseUtils.TestConnectionAsync(request, cancellationToken);
        }
    }
}
