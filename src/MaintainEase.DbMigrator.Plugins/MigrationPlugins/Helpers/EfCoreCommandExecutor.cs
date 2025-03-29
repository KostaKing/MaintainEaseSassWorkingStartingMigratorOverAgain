using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Plugins.MigrationPlugins.Helpers
{
    /// <summary>
    /// Helper for executing EF Core commands
    /// </summary>
    public class EfCoreCommandExecutor
    {
        private readonly ILogger _logger;

        public EfCoreCommandExecutor(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Execute an EF Core command
        /// </summary>
        public async Task<MigrationResult> ExecuteEfCoreCommandAsync(string scriptPath, CancellationToken cancellationToken)
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

        /// <summary>
        /// Helper method to get migration status
        /// </summary>
        public async Task<bool> GetMigrationStatus(string scriptPath, MigrationStatus status, CancellationToken cancellationToken)
        {
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

            if (process.ExitCode != 0)
            {
                status.ErrorMessage = $"Failed to get migration status: {error}";
                _logger?.LogError("Error getting migration status: {Error}", error);
                return false;
            }

            // Parse the migrations list output
            ParseMigrationsList(output, status);
            return true;
        }

        /// <summary>
        /// Helper method to find the most recent MSBuild log file
        /// </summary>
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
        /// Helper method to parse migration status output from EF Core
        /// </summary>
        public async Task<bool> ParseMigrationsOutput(string scriptPath, MigrationStatus status, CancellationToken cancellationToken)
        {
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

            if (process.ExitCode != 0)
            {
                status.ErrorMessage = $"Failed to get migration status: {error}";
                _logger?.LogError("Error getting migration status: {Error}", error);
                return false;
            }

            // Parse the migrations list output
            ParseMigrationsList(output, status);
            return true;
        }

        /// <summary>
        /// Find migration files created by EF Core
        /// </summary>
        public async Task<List<string>> FindMigrationFilesAsync(string directoryPath, string migrationName)
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
        /// Create a basic migration file when EF Core tools are not available
        /// </summary>
        public async Task<MigrationResult> CreateBasicMigrationAsync(MigrationRequest request, string outputDir, string migrationId, CancellationToken cancellationToken)
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
    }
}
