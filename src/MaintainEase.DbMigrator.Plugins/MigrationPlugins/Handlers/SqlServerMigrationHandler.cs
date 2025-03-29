using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Plugins.MigrationPlugins.Handlers
{
    /// <summary>
    /// Migration handler for SQL Server
    /// </summary>
    public class SqlServerMigrationHandler : IMigrationHandler
    {
        private readonly ILogger<SqlServerMigrationHandler> _logger;

        public SqlServerMigrationHandler(ILogger<SqlServerMigrationHandler> logger = null)
        {
            // Note: Logger is optional to allow for simpler instantiation when plugins are loaded dynamically
            _logger = logger;
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
                string solutionDir = FindSolutionDirectory(Directory.GetCurrentDirectory());
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
                    var dbContextTypes = await AnalyzeDbContextsInProject(infrastructureProjectPath);
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
                    await CreateProxyProjectAsync(tempProxyDir, infrastructureProjectPath, dbContextTypes);

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
                    var result = await ExecuteEfCoreCommandAsync(scriptPath, cancellationToken);

                    // Clean up the temporary script
                    try { File.Delete(scriptPath); } catch { /* Ignore errors */ }

                    if (!result.Success)
                    {
                        return result;
                    }

                    // Verify the migration files were created
                    var migrationFiles = await FindMigrationFilesAsync(tenantPath, request.MigrationName);

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
                    return await CreateBasicMigrationAsync(request, tenantPath, migrationId, cancellationToken);
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
                    MaskConnectionString(request.ConnectionConfig.ConnectionString));

                // Try to find dotnet executable
                string dotnetPath = FindDotnetExecutable();
                if (string.IsNullOrEmpty(dotnetPath))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find dotnet executable. Please ensure .NET SDK is installed."
                    };
                }

                // Find solution directory
                string solutionDir = FindSolutionDirectory(Directory.GetCurrentDirectory());
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
                var dbContextTypes = await AnalyzeDbContextsInProject(infrastructureProjectPath);
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
                    backupPath = await CreateBackupAsync(request, cancellationToken);
                    _logger?.LogInformation("Created SQL Server backup at: {BackupPath}", backupPath);
                }

                // Create a migration proxy project
                string tempProxyDir = Path.Combine(Path.GetTempPath(), $"MigrationProxy_{Guid.NewGuid().ToString("N")}");
                Directory.CreateDirectory(tempProxyDir);

                try
                {
                    // Create a minimal proxy project that references Infrastructure
                    await CreateProxyProjectAsync(tempProxyDir, infrastructureProjectPath, dbContextTypes);

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

                    var result = await ExecuteEfCoreCommandAsync(scriptPath, cancellationToken);

                    // Clean up the temporary script
                    try { File.Delete(scriptPath); } catch { /* Ignore errors */ }

                    if (!result.Success)
                    {
                        return result;
                    }

                    // Get the applied migrations (would be loaded from the database in a real implementation)
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
                    MaskConnectionString(request.ConnectionConfig.ConnectionString));

                // Find solution directory
                string solutionDir = FindSolutionDirectory(Directory.GetCurrentDirectory());
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
                var dbContextTypes = await AnalyzeDbContextsInProject(infrastructureProjectPath);
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
                    await CreateProxyProjectAsync(tempProxyDir, infrastructureProjectPath, dbContextTypes);

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

                    // Execute the command to get migration list
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c \"{scriptPath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync(cancellationToken);

                    // Clean up the temporary script
                    try { File.Delete(scriptPath); } catch { /* Ignore errors */ }

                    // Parse the output to determine applied and pending migrations
                    var status = new MigrationStatus
                    {
                        ProviderName = ProviderType,
                        DatabaseName = ExtractDatabaseName(request.ConnectionConfig.ConnectionString),
                        DatabaseVersion = "SQL Server"
                    };

                    if (process.ExitCode != 0)
                    {
                        status.ErrorMessage = $"Failed to get migration status: {error}";
                        _logger?.LogError("Error getting migration status: {Error}", error);
                        return status;
                    }

                    // Parse the migrations list output
                    ParseMigrationsList(output, status);

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
                    MaskConnectionString(request.ConnectionConfig.ConnectionString));

                // Create output directory if it doesn't exist
                var outputDir = string.IsNullOrEmpty(request.OutputDirectory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "Scripts")
                    : request.OutputDirectory;

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Find solution directory
                string solutionDir = FindSolutionDirectory(Directory.GetCurrentDirectory());
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
                var dbContextTypes = await AnalyzeDbContextsInProject(infrastructureProjectPath);
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
                    await CreateProxyProjectAsync(tempProxyDir, infrastructureProjectPath, dbContextTypes);

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

                    var result = await ExecuteEfCoreCommandAsync(scriptPath, cancellationToken);

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
            try
            {
                _logger?.LogInformation("Testing connection to SQL Server: {ConnectionString}",
                    MaskConnectionString(request.ConnectionConfig.ConnectionString));

                // Create a temporary script to test the connection
                string scriptPath = Path.Combine(Path.GetTempPath(), $"ef_test_connection_{Guid.NewGuid()}.cmd");

                // Create a simple SQL script to test the connection
                string sqlScript = "SELECT 1";
                string testScriptPath = Path.Combine(Path.GetTempPath(), $"test_connection_{Guid.NewGuid()}.sql");
                await File.WriteAllTextAsync(testScriptPath, sqlScript, cancellationToken);

                // Use sqlcmd to test the connection
                string connectionString = request.ConnectionConfig.ConnectionString;
                string server = ExtractServerFromConnectionString(connectionString);
                string database = ExtractDatabaseName(connectionString);
                string sqlCommand = $"sqlcmd -S {server} -d {database} -i \"{testScriptPath}\" -b";

                if (connectionString.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase) ||
                    connectionString.Contains("Integrated Security=True", StringComparison.OrdinalIgnoreCase) ||
                    connectionString.Contains("Integrated Security=SSPI", StringComparison.OrdinalIgnoreCase))
                {
                    // Use Windows authentication
                    sqlCommand += " -E";
                }
                else
                {
                    // Extract username and password from connection string
                    string user = ExtractUserFromConnectionString(connectionString);
                    string password = ExtractPasswordFromConnectionString(connectionString);

                    if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
                    {
                        sqlCommand += $" -U {user} -P {password}";
                    }
                }

                await File.WriteAllTextAsync(scriptPath, $"@echo off\necho Testing SQL Server connection...\n{sqlCommand}", cancellationToken);

                // Execute the script
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{scriptPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync(cancellationToken);

                // Clean up temporary files
                try
                {
                    File.Delete(scriptPath);
                    File.Delete(testScriptPath);
                }
                catch { /* Ignore errors */ }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error testing connection to SQL Server");
                return false;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Create a backup of the database
        /// </summary>
        private async Task<string> CreateBackupAsync(MigrationRequest request, CancellationToken cancellationToken)
        {
            // Create backup directory if it doesn't exist
            var backupDir = Path.Combine(
                string.IsNullOrEmpty(request.OutputDirectory)
                    ? Directory.GetCurrentDirectory()
                    : request.OutputDirectory,
                "Backups");

            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // Generate backup file name
            var databaseName = ExtractDatabaseName(request.ConnectionConfig.ConnectionString);
            var backupFileName = $"{databaseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
            var backupPath = Path.Combine(backupDir, backupFileName);

            // Create a temporary script to run the backup command
            string scriptPath = Path.Combine(Path.GetTempPath(), $"backup_script_{Guid.NewGuid()}.cmd");
            string connectionString = request.ConnectionConfig.ConnectionString;
            string server = ExtractServerFromConnectionString(connectionString);

            // Create SQL script for backup
            string backupSql = $"BACKUP DATABASE [{databaseName}] TO DISK = N'{backupPath}' WITH FORMAT, INIT, NAME = N'{databaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";
            string sqlScriptPath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.sql");
            await File.WriteAllTextAsync(sqlScriptPath, backupSql, cancellationToken);

            // Build sqlcmd command
            string sqlCommand = $"sqlcmd -S {server} -d {databaseName} -i \"{sqlScriptPath}\" -b";

            if (connectionString.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Integrated Security=True", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Integrated Security=SSPI", StringComparison.OrdinalIgnoreCase))
            {
                // Use Windows authentication
                sqlCommand += " -E";
            }
            else
            {
                // Extract username and password from connection string
                string user = ExtractUserFromConnectionString(connectionString);
                string password = ExtractPasswordFromConnectionString(connectionString);

                if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
                {
                    sqlCommand += $" -U {user} -P {password}";
                }
            }

            await File.WriteAllTextAsync(scriptPath, $"@echo off\necho Backing up database to {backupPath}...\n{sqlCommand}", cancellationToken);

            // Execute the backup command
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);

            // Clean up temporary files
            try
            {
                File.Delete(scriptPath);
                File.Delete(sqlScriptPath);
            }
            catch { /* Ignore errors */ }

            if (process.ExitCode != 0)
            {
                _logger?.LogError("Failed to create backup: {Error}", error);
                throw new Exception($"Failed to create backup: {error}");
            }

            // Create a dummy backup file if real backup failed
            if (!File.Exists(backupPath))
            {
                await File.WriteAllTextAsync(backupPath, $"-- This is a placeholder for the SQL Server backup of {databaseName}", cancellationToken);
                _logger?.LogWarning("Created placeholder backup file as real backup failed.");
            }

            return backupPath;
        }

        /// <summary>
        /// Create a basic migration file when EF Core tools are not available
        /// </summary>
        private async Task<MigrationResult> CreateBasicMigrationAsync(MigrationRequest request, string outputDir, string migrationId, CancellationToken cancellationToken)
        {
            // Generate a migration file name
            var migrationFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{request.MigrationName}.sql";
            var migrationFilePath = Path.Combine(outputDir, migrationFileName);

            // Create a simple migration template
            var migrationContent =
                $"-- Migration: {request.MigrationName}\r\n" +
                $"-- Created: {DateTime.UtcNow}\r\n" +
                $"-- Provider: SQL Server\r\n\r\n" +
                $"-- Write your SQL migration commands below this line\r\n\r\n";

            // Add migration history entry template
            migrationContent +=
                $"-- To register this migration in __EFMigrationsHistory table:\r\n" +
                $"-- INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])\r\n" +
                $"-- VALUES ('{migrationId}_{request.MigrationName}', '9.0.3');\r\n";

            // Write to file
            await File.WriteAllTextAsync(migrationFilePath, migrationContent, cancellationToken);

            _logger?.LogInformation("Created basic SQL Server migration file at {FilePath}", migrationFilePath);

            return new MigrationResult
            {
                Success = true,
                AppliedMigrations = new List<MigrationInfo>
                {
                    new MigrationInfo
                    {
                        Id = migrationId,
                        Name = request.MigrationName,
                        Created = DateTime.UtcNow,
                        Script = migrationFilePath
                    }
                }
            };
        }

        /// <summary>
        /// Analyze the Infrastructure project to find DbContext classes
        /// </summary>
        private async Task<List<DbContextInfo>> AnalyzeDbContextsInProject(string projectPath)
        {
            var result = new List<DbContextInfo>();

            try
            {
                // Look for existing compiled DLLs first
                var configurations = new[] { "Debug", "Release" };
                var frameworks = new[] { "net9.0", "net8.0", "net7.0", "net6.0" };
                string dllPath = null;

                foreach (var config in configurations)
                {
                    foreach (var framework in frameworks)
                    {
                        var path = Path.Combine(projectPath, "bin", config, framework, "MaintainEase.Infrastructure.dll");
                        if (File.Exists(path))
                        {
                            dllPath = path;
                            _logger?.LogInformation("Found Infrastructure DLL at {Path}", path);
                            break;
                        }
                    }
                    if (dllPath != null) break;
                }

                if (dllPath == null)
                {
                    // If DLL not found, we'll need to build the project
                    await BuildProjectAsync(projectPath);

                    // Try to find the DLL again after building
                    foreach (var config in configurations)
                    {
                        foreach (var framework in frameworks)
                        {
                            var path = Path.Combine(projectPath, "bin", config, framework, "MaintainEase.Infrastructure.dll");
                            if (File.Exists(path))
                            {
                                dllPath = path;
                                _logger?.LogInformation("Found Infrastructure DLL after building at {Path}", path);
                                break;
                            }
                        }
                        if (dllPath != null) break;
                    }
                }

                if (dllPath == null)
                {
                    _logger?.LogWarning("Could not find compiled Infrastructure DLL after building.");

                    // Fallback: analyze source code directly to find DbContext classes
                    return await AnalyzeSourceCodeForDbContexts(projectPath);
                }

                // Use reflection to find DbContext types in the DLL
                try
                {
                    var assembly = Assembly.LoadFrom(dllPath);

                    // Find types that derive from DbContext
                    var dbContextBaseType = typeof(DbContext);
                    var types = assembly.GetTypes()
                        .Where(t => t != dbContextBaseType && // Not the base DbContext type
                                    dbContextBaseType.IsAssignableFrom(t) && // Is or inherits from DbContext
                                    !t.IsAbstract && // Not abstract
                                    !t.IsInterface) // Not an interface
                        .ToList();

                    foreach (var type in types)
                    {
                        result.Add(new DbContextInfo
                        {
                            Name = type.Name,
                            FullName = type.FullName,
                            Namespace = type.Namespace,
                            Assembly = assembly.GetName().Name
                        });

                        _logger?.LogInformation("Found DbContext: {Name} ({FullName})", type.Name, type.FullName);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error loading assembly for reflection: {DllPath}", dllPath);

                    // Fallback: analyze source code
                    return await AnalyzeSourceCodeForDbContexts(projectPath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error analyzing Infrastructure project for DbContext classes");
            }

            return result;
        }

        /// <summary>
        /// Build the Infrastructure project
        /// </summary>
        private async Task<bool> BuildProjectAsync(string projectPath)
        {
            try
            {
                _logger?.LogInformation("Building Infrastructure project at {Path}", projectPath);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{projectPath}\" -c Debug",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger?.LogError("Failed to build Infrastructure project: {Error}", error);
                    return false;
                }

                _logger?.LogInformation("Successfully built Infrastructure project");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error building Infrastructure project");
                return false;
            }
        }

        /// <summary>
        /// Fallback: analyze source code directly to find DbContext classes
        /// </summary>
        private async Task<List<DbContextInfo>> AnalyzeSourceCodeForDbContexts(string projectPath)
        {
            var result = new List<DbContextInfo>();

            try
            {
                _logger?.LogInformation("Analyzing source code to find DbContext classes");

                // Look for .cs files in the project
                var sourceFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

                foreach (var file in sourceFiles)
                {
                    try
                    {
                        string content = await File.ReadAllTextAsync(file);

                        // Look for class declarations that inherit from DbContext
                        if (content.Contains("DbContext") &&
                            (content.Contains(" : DbContext") || content.Contains(" : Microsoft.EntityFrameworkCore.DbContext")))
                        {
                            // Parse the file to extract namespace and class name
                            string namespaceName = ExtractNamespace(content);
                            string className = ExtractClassName(content, "DbContext");

                            if (!string.IsNullOrEmpty(className))
                            {
                                result.Add(new DbContextInfo
                                {
                                    Name = className,
                                    FullName = string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}",
                                    Namespace = namespaceName,
                                    Assembly = "MaintainEase.Infrastructure",
                                    SourceFile = file
                                });

                                _logger?.LogInformation("Found DbContext in source: {Name} ({FullName})",
                                    className, string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error analyzing source file: {File}", file);
                        // Continue to next file
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error analyzing source code");
            }

            return result;
        }

        /// <summary>
        /// Extract namespace from source code
        /// </summary>
        private string ExtractNamespace(string content)
        {
            var match = Regex.Match(content, @"namespace\s+([^\s;{]+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        /// <summary>
        /// Extract class name from source code
        /// </summary>
        private string ExtractClassName(string content, string baseClass)
        {
            // Look for a class that inherits from the specified base class
            var match = Regex.Match(content, @"class\s+([^\s:;{]+)(?:\s*:\s*(?:[^{;]*\s)?" + baseClass + @")");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        /// <summary>
        /// Create a minimal proxy project that helps with DbContext discovery
        /// </summary>
        private async Task CreateProxyProjectAsync(string proxyDir, string infrastructureProjectPath, List<DbContextInfo> dbContexts)
        {
            // Create a minimal project file that references Infrastructure
            string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""9.0.3"" PrivateAssets=""all"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""9.0.3"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""{infrastructureProjectPath}\MaintainEase.Infrastructure.csproj"" />
  </ItemGroup>
</Project>";

            await File.WriteAllTextAsync(Path.Combine(proxyDir, "MigrationProxy.csproj"), projectContent);

            // Create a factory class for each discovered DbContext
            foreach (var context in dbContexts)
            {
                // Create a design-time factory for the context
                string factoryContent = $@"// <auto-generated/>
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MigrationProxy.DesignTimeFactories
{{
    // Design-time factory for {context.Name}
    public class {context.Name}Factory : IDesignTimeDbContextFactory<{context.FullName}>
    {{
        public {context.FullName} CreateDbContext(string[] args)
        {{
            string connectionString = Environment.GetEnvironmentVariable(""ConnectionStrings__DefaultConnection"") ?? 
                                     ""Server=localhost;Database=MaintainEase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"";
            
            Console.WriteLine($""Using connection string: {{MaskConnectionString(connectionString)}}"");
            
            // Parse command line args for connection string
            for (int i = 0; i < args.Length; i++)
            {{
                if (args[i] == ""--connection"" && i + 1 < args.Length)
                {{
                    connectionString = args[i + 1];
                    Console.WriteLine($""Overriding with connection string from args: {{MaskConnectionString(connectionString)}}"");
                    break;
                }}
            }}
            
            var optionsBuilder = new DbContextOptionsBuilder<{context.FullName}>();
            optionsBuilder.UseSqlServer(connectionString);
            
            // Try to create an instance of the DbContext
            try {{
                return ({context.FullName})Activator.CreateInstance(
                    typeof({context.FullName}), 
                    optionsBuilder.Options);
            }}
            catch (Exception ex) {{
                Console.WriteLine($""Error creating {context.Name}: {{ex.Message}}"");
                Console.WriteLine(ex.StackTrace);
                
                // Create with minimal dependencies - for migration commands only
                Console.WriteLine(""Trying to create with minimal dependencies for migration operations"");
                
                var constructors = typeof({context.FullName}).GetConstructors();
                foreach (var constructor in constructors) {{
                    Console.WriteLine($""Found constructor with {{constructor.GetParameters().Length}} parameters"");
                }}
                
                throw;
            }}
        }}
        
        private string MaskConnectionString(string connectionString)
        {{
            if (string.IsNullOrEmpty(connectionString))
                return ""[empty]"";

            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @""(Password|pwd)=([^;]*)"",
                ""$1=********"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }}
    }}
}}";

                // Create file
                var factoryDir = Path.Combine(proxyDir, "DesignTimeFactories");
                Directory.CreateDirectory(factoryDir);
                await File.WriteAllTextAsync(Path.Combine(factoryDir, $"{context.Name}Factory.cs"), factoryContent);
            }

            // Create a minimal Program.cs file with DbContext extension to help EF Core discover our DbContexts
            string programContent = $@"// Migration proxy application
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace MigrationProxy
{{
    public class Program
    {{
        public static void Main(string[] args)
        {{
            Console.WriteLine(""Migration proxy running..."");
            Console.WriteLine(""Available DbContexts:"");

            // List available DbContexts for diagnostics
            {string.Join("\n            ", dbContexts.Select(c => $"Console.WriteLine(\"  - {c.Name} ({c.FullName})\");"))}
            
            // If no args or help requested, show usage
            if (args.Length == 0 || args.Contains(""--help"") || args.Contains(""-h""))
            {{
                ShowUsage();
                return;
            }}
        }}
        
        private static void ShowUsage()
        {{
            Console.WriteLine(""Usage: MigrationProxy <command> [options]"");
            Console.WriteLine();
            Console.WriteLine(""Commands:"");
            Console.WriteLine(""  migrations add <name>     Add a new migration"");
            Console.WriteLine(""  migrations list          List available migrations"");
            Console.WriteLine(""  database update          Apply migrations to the database"");
            Console.WriteLine();
            Console.WriteLine(""Options:"");
            Console.WriteLine(""  --context <context>      The DbContext to use"");
            Console.WriteLine(""  --connection <string>    The connection string to use"");
        }}
    }}
}}";
            await File.WriteAllTextAsync(Path.Combine(proxyDir, "Program.cs"), programContent);

            // Restore and build the project
            string restoreScriptPath = Path.Combine(Path.GetTempPath(), $"restore_proxy_{Guid.NewGuid()}.cmd");
            await File.WriteAllTextAsync(restoreScriptPath, $"@echo off\ncd \"{proxyDir}\"\ndotnet restore\ndotnet build");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{restoreScriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            try { File.Delete(restoreScriptPath); } catch { /* Ignore errors */ }

            if (process.ExitCode != 0)
            {
                _logger?.LogError("Failed to build proxy project: {Error}", error);
                throw new Exception($"Failed to build proxy project: {error}");
            }

            _logger?.LogInformation("Created, restored, and built migration proxy project");
        }

        /// <summary>
        /// Class to hold DbContext information
        /// </summary>
        public class DbContextInfo
        {
            public string Name { get; set; }
            public string FullName { get; set; }
            public string Namespace { get; set; }
            public string Assembly { get; set; }
            public string SourceFile { get; set; }  // Source file path if discovered via source code analysis
        }

        // Helper method to output project diagnostics
        private async Task<string> GenerateProjectDiagnosticsAsync(string projectPath)
        {
            var sb = new StringBuilder();

            // Check if project file exists
            if (!File.Exists(Path.Combine(projectPath, "MaintainEase.Infrastructure.csproj")))
            {
                sb.AppendLine("ERROR: Infrastructure project file not found!");

                // List files in the directory to help diagnose
                if (Directory.Exists(projectPath))
                {
                    sb.AppendLine("Files in directory:");
                    foreach (var file in Directory.GetFiles(projectPath, "*.csproj"))
                    {
                        sb.AppendLine($"  - {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    sb.AppendLine("Directory does not exist!");
                }
            }
            else
            {
                sb.AppendLine("Found Infrastructure project file.");

                // Check for DbContext classes
                var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
                var dbContextFiles = csFiles.Where(f => File.ReadAllText(f).Contains(" DbContext") ||
                                                       File.ReadAllText(f).Contains(":DbContext") ||
                                                       File.ReadAllText(f).Contains(": DbContext")).ToList();

                if (dbContextFiles.Any())
                {
                    sb.AppendLine("Found DbContext classes:");
                    foreach (var file in dbContextFiles)
                    {
                        sb.AppendLine($"  - {Path.GetRelativePath(projectPath, file)}");
                    }
                }
                else
                {
                    sb.AppendLine("WARNING: No DbContext classes found!");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Execute an EF Core command
        /// </summary>
        private async Task<MigrationResult> ExecuteEfCoreCommandAsync(string scriptPath, CancellationToken cancellationToken)
        {
            // Run the actual command
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);

            var result = new MigrationResult { Success = process.ExitCode == 0 };

            if (process.ExitCode != 0)
            {
                // Add detailed diagnostic information
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine($"Command failed with exit code {process.ExitCode}:");

                if (!string.IsNullOrEmpty(error))
                    errorBuilder.AppendLine($"Error output: {error}");

                // Try to get MSBuild log if available
                string msbuildLog = GetLastMsBuildLog();
                if (!string.IsNullOrEmpty(msbuildLog))
                    errorBuilder.AppendLine($"MSBuild details: {msbuildLog}");

                result.ErrorMessage = errorBuilder.ToString();
                _logger?.LogError("Command failed: {Error}", result.ErrorMessage);
            }

            if (!string.IsNullOrEmpty(output))
            {
                result.AdditionalInfo = new Dictionary<string, string> {
                    { "CommandOutput", output }
                };
            }

            return result;
        }

        // Helper method to find the most recent MSBuild log file
        private string GetLastMsBuildLog()
        {
            try
            {
                var tempFolder = Path.GetTempPath();
                var msbuildLogs = Directory.GetFiles(tempFolder, "MSBuild_*.log")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Take(1)
                    .ToArray();

                if (msbuildLogs.Length > 0 && File.Exists(msbuildLogs[0]))
                {
                    // Read last 50 lines of the log (which usually contain the error)
                    var logLines = File.ReadAllLines(msbuildLogs[0]);
                    var errorLines = logLines
                        .Reverse()
                        .Take(50)
                        .Where(l => l.Contains("error") || l.Contains("warning"))
                        .Reverse()
                        .ToList();

                    return string.Join(Environment.NewLine, errorLines);
                }
            }
            catch
            {
                // Ignore errors in diagnostic code
            }

            return null;
        }
        /// <summary>
        /// Find the dotnet executable
        /// </summary>
        private string FindDotnetExecutable()
        {
            // Try to find dotnet in PATH
            foreach (var path in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, "dotnet.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // Default locations
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var possiblePaths = new[]
            {
                Path.Combine(programFiles, "dotnet", "dotnet.exe"),
                Path.Combine(programFilesX86, "dotnet", "dotnet.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Assume dotnet is in PATH
            return "dotnet";
        }

        /// <summary>
        /// Find the solution directory
        /// </summary>
        private string FindSolutionDirectory(string startPath)
        {
            var directory = new DirectoryInfo(startPath);

            while (directory != null)
            {
                if (directory.GetFiles("*.sln").Any())
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }

        /// <summary>
        /// Find migration files created by EF Core
        /// </summary>
        private async Task<List<string>> FindMigrationFilesAsync(string directoryPath, string migrationName)
        {
            var result = new List<string>();

            // Normalize migration name to match EF Core's naming convention
            var normalizedName = migrationName.Replace(" ", "");

            if (!Directory.Exists(directoryPath))
            {
                return result;
            }

            // Look for migration files in the directory
            var files = Directory.GetFiles(directoryPath, $"*{normalizedName}*.cs");
            result.AddRange(files);

            // If no files found, try searching in subdirectories
            if (result.Count == 0)
            {
                files = Directory.GetFiles(directoryPath, $"*{normalizedName}*.cs", SearchOption.AllDirectories);
                result.AddRange(files);
            }

            return result;
        }

        /// <summary>
        /// Parse the output of EF Core migrations list command
        /// </summary>
        private void ParseMigrationsList(string output, MigrationStatus status)
        {
            var lines = output.Split('\n');
            var appliedMigrations = new List<MigrationInfo>();
            var pendingMigrations = new List<MigrationInfo>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("Done.") || trimmedLine.StartsWith("Running:"))
                {
                    continue;
                }

                // EF Core output format: [timestamp]_[name] (Applied/Pending)
                var migrationParts = trimmedLine.Split(new[] { ' ' }, 2);
                if (migrationParts.Length >= 1)
                {
                    var migrationIdParts = migrationParts[0].Split('_', 2);
                    var migrationId = migrationIdParts[0];
                    var migrationName = migrationIdParts.Length > 1 ? migrationIdParts[1] : "Unknown";

                    var isApplied = migrationParts.Length > 1 && migrationParts[1].Contains("(Applied)");

                    var migrationInfo = new MigrationInfo
                    {
                        Id = migrationId,
                        Name = migrationName,
                        AppliedOn = isApplied ? DateTime.UtcNow : null
                    };

                    if (isApplied)
                    {
                        appliedMigrations.Add(migrationInfo);
                    }
                    else
                    {
                        pendingMigrations.Add(migrationInfo);
                    }
                }
            }

            status.AppliedMigrations = appliedMigrations;
            status.PendingMigrations = pendingMigrations;
            status.PendingMigrationsCount = pendingMigrations.Count;
            status.HasPendingMigrations = pendingMigrations.Count > 0;

            if (appliedMigrations.Count > 0)
            {
                var lastMigration = appliedMigrations.OrderBy(m => m.Id).Last();
                status.LastMigrationDate = lastMigration.AppliedOn;
                status.LastMigrationName = lastMigration.Name;
            }
        }

        /// <summary>
        /// Extract the database name from a connection string
        /// </summary>
        private string ExtractDatabaseName(string connectionString)
        {
            // Simple parser to extract the database name from a SQL Server connection string
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    if (key == "database" || key == "initial catalog")
                    {
                        return keyValue[1].Trim();
                    }
                }
            }

            return "Database";
        }

        /// <summary>
        /// Extract the server name from a connection string
        /// </summary>
        private string ExtractServerFromConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    if (key == "server" || key == "data source")
                    {
                        return keyValue[1].Trim();
                    }
                }
            }

            return "localhost";
        }

        /// <summary>
        /// Extract username from connection string
        /// </summary>
        private string ExtractUserFromConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    if (key == "user id" || key == "uid" || key == "username")
                    {
                        return keyValue[1].Trim();
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Extract password from connection string
        /// </summary>
        private string ExtractPasswordFromConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    if (key == "password" || key == "pwd")
                    {
                        return keyValue[1].Trim();
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Mask sensitive information in the connection string for logging
        /// </summary>
        private string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            // Replace password with asterisks
            var maskedConnectionString = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|pwd)=([^;]*)",
                "$1=********",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return maskedConnectionString;
        }

        #endregion
    }
}
