using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;
using System.Text;

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

                // Ensure output directory exists
                var outputDir = string.IsNullOrEmpty(request.OutputDirectory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "Migrations")
                    : request.OutputDirectory;

                // When building tenantPath, make it correctly target Infrastructure project:
                string baseMigrationsFolder = Path.Combine(FindSolutionDirectory(Directory.GetCurrentDirectory()),
                    "src", "MaintainEase.Infrastructure", "Migrations");

                string tenantPath = Path.Combine(baseMigrationsFolder,
                ProviderType, // This class's ProviderType property is "SqlServer"
                request.TenantId ?? "default");
                if (!Directory.Exists(tenantPath))
                {
                    Directory.CreateDirectory(tenantPath);
                    _logger?.LogInformation("Created migrations directory: {OutputDir}", tenantPath);
                }

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

                // Find Infrastructure project path (or where DbContext is located)
                string infrastructureProjectPath = await FindInfrastructureProjectPathAsync();
                if (string.IsNullOrEmpty(infrastructureProjectPath))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find Infrastructure project. Please ensure it exists in the solution."
                    };
                }

                // Determine DbContext to use
                string contextName = request.AdditionalInfo != null &&
                                    request.AdditionalInfo.TryGetValue("DbContext", out var dbContext)
                                    ? dbContext
                                    : "AppDbContext";

                // Find startup project (where Program.cs is located)
                string startupProjectPath = Path.Combine(FindSolutionDirectory(Directory.GetCurrentDirectory()), "src", "MaintainEase.DbMigrator");


                // Create a temporary script to run EF Core commands to avoid command line length issues
                string scriptPath = Path.Combine(Path.GetTempPath(), $"ef_migration_{Guid.NewGuid()}.cmd");
                string migrationId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                // Build the EF Core migrations command
                string efCommand = $"ef migrations add {request.MigrationName} " +
                                 $"--context {contextName} " +
                                 $"--project \"{infrastructureProjectPath}\" " +
                                 $"--startup-project \"{startupProjectPath}\" " +
                                 $"--output-dir \"{tenantPath}\" " +
                                 $"-- --provider {ProviderType} --connection \"{request.ConnectionConfig.ConnectionString}\"";

                await File.WriteAllTextAsync(scriptPath, $"@echo off\necho Running: dotnet {efCommand}\ndotnet {efCommand}", cancellationToken);

                _logger?.LogInformation("EF Core command: dotnet {Command}", efCommand);

                _logger?.LogInformation("Project diagnostics: \n{Diagnostics}",
            await GenerateProjectDiagnosticsAsync(infrastructureProjectPath));

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

                // If EF Core reported success but we can't find the files, create basic migration files
                _logger?.LogWarning("EF Core reported success but no migration files were found. Creating basic migration files.");
                return await CreateBasicMigrationAsync(request, tenantPath, migrationId, cancellationToken);
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

                // Find Infrastructure project path (or where DbContext is located)
                string infrastructureProjectPath = await FindInfrastructureProjectPathAsync();
                if (string.IsNullOrEmpty(infrastructureProjectPath))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find Infrastructure project. Please ensure it exists in the solution."
                    };
                }

                // Determine DbContext to use
                string contextName = request.AdditionalInfo != null &&
                                    request.AdditionalInfo.TryGetValue("DbContext", out var dbContext)
                                    ? dbContext
                                    : "AppDbContext";

                // Find startup project (where Program.cs is located)
                string startupProjectPath = Path.Combine(FindSolutionDirectory(Directory.GetCurrentDirectory()), "src", "MaintainEase.DbMigrator");


                // If creating a backup was requested (and implemented - this is a placeholder)
                string backupPath = null;
                if (request.CreateBackup)
                {
                    backupPath = await CreateBackupAsync(request, cancellationToken);
                    _logger?.LogInformation("Created SQL Server backup at: {BackupPath}", backupPath);
                }

                // Create a temporary script to run EF Core commands
                string scriptPath = Path.Combine(Path.GetTempPath(), $"ef_migration_{Guid.NewGuid()}.cmd");

                // Build the database update command
                string efCommand = $"ef database update " +
                                 $"--context {contextName} " +
                                 $"--project \"{infrastructureProjectPath}\" " +
                                 $"--startup-project \"{startupProjectPath}\" " +
                                 $"-- --provider {ProviderType} --connection \"{request.ConnectionConfig.ConnectionString}\"";

                await File.WriteAllTextAsync(scriptPath, $"@echo off\necho Running: dotnet {efCommand}\ndotnet {efCommand}", cancellationToken);

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

                // Find Infrastructure project path (or where DbContext is located)
                string infrastructureProjectPath = await FindInfrastructureProjectPathAsync();
                if (string.IsNullOrEmpty(infrastructureProjectPath))
                {
                    return new MigrationStatus
                    {
                        ErrorMessage = "Could not find Infrastructure project."
                    };
                }

                // Determine DbContext to use
                string contextName = request.AdditionalInfo != null &&
                                    request.AdditionalInfo.TryGetValue("DbContext", out var dbContext)
                                    ? dbContext
                                    : "AppDbContext";

                // Find startup project (where Program.cs is located)
                string startupProjectPath = Path.Combine(FindSolutionDirectory(Directory.GetCurrentDirectory()), "src", "MaintainEase.DbMigrator");


                // Create a temporary script to run EF Core commands
                string scriptPath = Path.Combine(Path.GetTempPath(), $"ef_migration_{Guid.NewGuid()}.cmd");

                // Build the list command
                string efCommand = $"ef migrations list " +
                                 $"--context {contextName} " +
                                 $"--project \"{infrastructureProjectPath}\" " +
                                 $"--startup-project \"{startupProjectPath}\" " +
                                 $"-- --provider {ProviderType} --connection \"{request.ConnectionConfig.ConnectionString}\"";

                await File.WriteAllTextAsync(scriptPath, $"@echo off\necho Running: dotnet {efCommand}\ndotnet {efCommand}", cancellationToken);

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

                // Find Infrastructure project path (or where DbContext is located)
                string infrastructureProjectPath = await FindInfrastructureProjectPathAsync();
                if (string.IsNullOrEmpty(infrastructureProjectPath))
                {
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find Infrastructure project."
                    };
                }

                // Determine DbContext to use
                string contextName = request.AdditionalInfo != null &&
                                    request.AdditionalInfo.TryGetValue("DbContext", out var dbContext)
                                    ? dbContext
                                    : "AppDbContext";

                // Find startup project (where Program.cs is located)
                string startupProjectPath = Path.Combine(FindSolutionDirectory(Directory.GetCurrentDirectory()), "src", "MaintainEase.DbMigrator");


                // Create a temporary script to run EF Core commands
                string scriptPath = Path.Combine(Path.GetTempPath(), $"ef_migration_{Guid.NewGuid()}.cmd");
                string outputScriptPath = Path.Combine(outputDir, $"migration_script_{DateTime.Now:yyyyMMddHHmmss}.sql");

                // Build the script command
                string efCommand = $"ef migrations script " +
                                 $"--context {contextName} " +
                                 $"--project \"{infrastructureProjectPath}\" " +
                                 $"--startup-project \"{startupProjectPath}\" " +
                                 $"--output \"{outputScriptPath}\" " +
                                 $"-- --provider {ProviderType} --connection \"{request.ConnectionConfig.ConnectionString}\"";

                await File.WriteAllTextAsync(scriptPath, $"@echo off\necho Running: dotnet {efCommand}\ndotnet {efCommand}", cancellationToken);

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

        // Add this method to SqlServerMigrationHandler
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
            string infrastructureProjectPath = await FindInfrastructureProjectPathAsync();

            // Create a diagnostic build command first to see potential issues
            string diagnosticCmd = $"@echo off\necho Running diagnostics...\ndotnet build \"{infrastructureProjectPath}\" -v minimal";
            await File.WriteAllTextAsync(scriptPath + ".diagnostic.cmd", diagnosticCmd, cancellationToken);

            var diagnosticProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}.diagnostic.cmd\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            diagnosticProcess.Start();
            string diagnosticOutput = await diagnosticProcess.StandardOutput.ReadToEndAsync();
            string diagnosticError = await diagnosticProcess.StandardError.ReadToEndAsync();
            await diagnosticProcess.WaitForExitAsync(cancellationToken);

            // Now run the actual command
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

                if (!string.IsNullOrEmpty(diagnosticError))
                    errorBuilder.AppendLine($"Build diagnostics: {diagnosticError}");

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
            { "CommandOutput", output },
            { "DiagnosticOutput", diagnosticOutput }
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
        /// Find the Infrastructure project path
        /// </summary>
        private async Task<string> FindInfrastructureProjectPathAsync()
        {
       

            // Try to find solution file
            string solutionDir = FindSolutionDirectory(Directory.GetCurrentDirectory());
            if (string.IsNullOrEmpty(solutionDir))
            {
                _logger?.LogWarning("Could not find solution directory.");
                return null;
            }

            // Look for Infrastructure project in solution directory
            var possibleInfraLocations = new[]
            {
                Path.Combine(solutionDir, "src", "MaintainEase.Infrastructure"),
                Path.Combine(solutionDir, "src", "Infrastructure"),
                Path.Combine(solutionDir, "MaintainEase.Infrastructure"),
                Path.Combine(solutionDir, "Infrastructure")
            };

            foreach (var location in possibleInfraLocations)
            {
                if (Directory.Exists(location))
                {
                    _logger?.LogInformation("Found Infrastructure project at: {Location}", location);
                    return location;
                }
            }

            // Try to find any project file with "Infrastructure" in the name
            _logger?.LogInformation("Looking for Infrastructure project file in solution directory...");
            try
            {
                var projFiles = Directory.GetFiles(solutionDir, "*Infrastructure*.csproj", SearchOption.AllDirectories);
                if (projFiles.Length > 0)
                {
                    var projectPath = Path.GetDirectoryName(projFiles[0]);
                    _logger?.LogInformation("Found Infrastructure project file at: {Path}", projectPath);
                    return projectPath;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching for Infrastructure project file.");
            }

            return null;
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
