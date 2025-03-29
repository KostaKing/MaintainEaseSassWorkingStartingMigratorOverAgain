using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Microsoft.Extensions.Options;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;

namespace MaintainEase.DbMigrator.Commands.Migration
{
    /// <summary>
    /// Command to create a new migration
    /// </summary>
    public class CreateMigrationCommand : AsyncCommand<CreateMigrationCommand.Settings>
    {
        private readonly IServiceProvider _serviceProvider;
        private const string MIGRATIONS_BASE_DIR = "Migrations";

        public CreateMigrationCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public class Settings : CommandSettings
        {
            [CommandOption("-t|--tenant")]
            [Description("Tenant to create migration for")]
            public string TenantIdentifier { get; set; } = "default";

            [CommandOption("-c|--context")]
            [Description("DbContext to use (App or Base)")]
            public string DbContext { get; set; } = "App";

            [CommandOption("-p|--provider")]
            [Description("Database provider to use (SqlServer or PostgreSQL)")]
            public string Provider { get; set; }

            [CommandOption("-o|--output-dir")]
            [Description("Custom output directory for migration files")]
            public string OutputDir { get; set; }

            [CommandArgument(0, "<name>")]
            [Description("Name of the migration")]
            public string MigrationName { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            try
            {
                // Create nice header
                SafeMarkup.Banner("Create Migration", "green");
                AnsiConsole.MarkupLine($"[yellow]Migration name:[/] {settings.MigrationName}");
                AnsiConsole.MarkupLine($"[yellow]Tenant:[/] {settings.TenantIdentifier}");
                AnsiConsole.MarkupLine($"[yellow]DbContext:[/] {settings.DbContext}");

                // Get necessary configuration
                var dbMigratorSettings = _serviceProvider.GetRequiredService<IOptions<DbMigratorSettings>>().Value;

                // Determine provider
                string provider = settings.Provider ?? dbMigratorSettings.DatabaseProvider;
                AnsiConsole.MarkupLine($"[yellow]Provider:[/] {provider}");

                // Use correct context name based on settings
                string contextName = settings.DbContext.EndsWith("DbContext")
                    ? settings.DbContext
                    : $"{settings.DbContext}DbContext";

                // Find Infrastructure project
                var (infrastructureProjectPath, startupProjectPath) = await FindProjectPaths();
                if (string.IsNullOrEmpty(infrastructureProjectPath))
                {
                    SafeMarkup.Error("Could not find Infrastructure project. Migration creation failed.");
                    return 1;
                }

                // Create organized migrations directory structure
                string migrationsStructure = await SetupMigrationsDirectory(
                    settings,
                    infrastructureProjectPath,
                    provider);

                // Build connection string
                string connectionString = GetConnectionString(settings.TenantIdentifier, dbMigratorSettings);

                // First attempt: Create migration with output dir specified
                if (!await TryCreateMigrationWithEFCore(
                    settings,
                    contextName,
                    infrastructureProjectPath,
                    startupProjectPath,
                    migrationsStructure,
                    provider,
                    connectionString))
                {
                    // Second attempt: Create migration without output dir and copy files
                    if (!await TryCreateAndCopyMigration(
                        settings,
                        contextName,
                        infrastructureProjectPath,
                        startupProjectPath,
                        migrationsStructure,
                        provider,
                        connectionString))
                    {
                        return 1;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                SafeMarkup.Error("An unexpected error occurred");
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }

        /// <summary>
        /// Attempts to find the Infrastructure project and startup project paths
        /// </summary>
        private async Task<(string InfrastructurePath, string StartupPath)> FindProjectPaths()
        {
            return await AnsiConsole.Status().StartAsync("Locating projects...", async ctx =>
            {
                string startupProjectPath = AppContext.BaseDirectory;
                ctx.Status("Locating startup project...");
                AnsiConsole.MarkupLine($"[yellow]Startup project path:[/] {startupProjectPath}");

                // Find Infrastructure project using multiple fallback paths
                string? infrastructureProjectPath = null;
                var possibleLocations = new[] {
                    Path.Combine(startupProjectPath, "..", "..", "..", "MaintainEase.Infrastructure"),
                    Path.GetFullPath(Path.Combine(startupProjectPath, "..", "..", "..", "src", "MaintainEase.Infrastructure")),
                    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "MaintainEase.Infrastructure")),
                    // Also try solution-relative paths
                    Path.GetFullPath(Path.Combine(FindSolutionDirectory(Directory.GetCurrentDirectory()), "src", "MaintainEase.Infrastructure"))
                };

                ctx.Status("Searching for Infrastructure project...");
                foreach (var location in possibleLocations)
                {
                    if (Directory.Exists(location))
                    {
                        infrastructureProjectPath = location;
                        AnsiConsole.MarkupLine($"[green]Found Infrastructure project at:[/] {infrastructureProjectPath}");
                        break;
                    }
                }

                // If not found, search for any Infrastructure project
                if (infrastructureProjectPath == null)
                {
                    ctx.Status("Searching for any Infrastructure project...");
                    var searchRoot = Directory.GetCurrentDirectory();
                    var solutionDir = FindSolutionDirectory(searchRoot);
                    var searchPath = string.IsNullOrEmpty(solutionDir) ? searchRoot : solutionDir;

                    var projFiles = Directory.GetFiles(searchPath, "*Infrastructure*.csproj", SearchOption.AllDirectories);
                    if (projFiles.Length > 0)
                    {
                        infrastructureProjectPath = Path.GetDirectoryName(projFiles[0]);
                        AnsiConsole.MarkupLine($"[green]Found Infrastructure project at:[/] {infrastructureProjectPath}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Error: Could not find the Infrastructure project[/]");
                    }
                }

                return (infrastructureProjectPath, startupProjectPath);
            });
        }

        /// <summary>
        /// Sets up the organized migrations directory structure
        /// </summary>
        private async Task<string> SetupMigrationsDirectory(Settings settings, string infrastructureProjectPath, string provider)
        {
            return await AnsiConsole.Status().StartAsync("Setting up migrations directory...", async ctx =>
            {
                string migrationsBaseDir = MIGRATIONS_BASE_DIR;

                // Use custom output dir if provided
                if (!string.IsNullOrEmpty(settings.OutputDir))
                {
                    migrationsBaseDir = settings.OutputDir;
                }

                // Create full path with organized structure: Migrations/[Provider]/[Tenant]
                string migrationsRootDir = Path.Combine(infrastructureProjectPath, migrationsBaseDir);
                string providerDir = Path.Combine(migrationsRootDir, provider);
                string tenantDir = Path.Combine(providerDir, settings.TenantIdentifier);

                // Create directories
                ctx.Status($"Creating directories: {tenantDir}");
                Directory.CreateDirectory(tenantDir);

                AnsiConsole.MarkupLine($"[green]Created migrations directory structure:[/]");
                AnsiConsole.MarkupLine($"[grey]- {migrationsRootDir}[/]");
                AnsiConsole.MarkupLine($"[grey]  └─ {provider}[/]");
                AnsiConsole.MarkupLine($"[grey]     └─ {settings.TenantIdentifier}[/]");

                return tenantDir;
            });
        }

        /// <summary>
        /// Try to create migration with EF Core, specifying the output directory
        /// </summary>
        private async Task<bool> TryCreateMigrationWithEFCore(
            Settings settings,
            string contextName,
            string infrastructureProjectPath,
            string startupProjectPath,
            string outputDir,
            string provider,
            string connectionString)
        {
            return await AnsiConsole.Status().StartAsync("Creating migration with EF Core...", async ctx =>
            {
                string outputDirParam = $"--output-dir \"{Path.GetRelativePath(infrastructureProjectPath, outputDir)}\"";

                // Build the EF Core command with output directory specified
                string efCoreCommand = $"migrations add {settings.MigrationName} " +
                                      $"--context {contextName} " +
                                      $"{outputDirParam} " +
                                      $"--project \"{infrastructureProjectPath}\" " +
                                      $"--startup-project \"{startupProjectPath}\" " +
                                      $"-- --provider {provider} --connection \"{connectionString}\"";

                ctx.Status($"Running: dotnet ef {efCoreCommand}");
                AnsiConsole.MarkupLine("[yellow]Executing command:[/]");
                AnsiConsole.WriteLine($"dotnet ef {efCoreCommand}");

                // Execute the command
                var result = await ExecuteCommand("dotnet", $"ef {efCoreCommand}", infrastructureProjectPath);

                if (result.ExitCode == 0)
                {
                    SafeMarkup.Success($"Migration '{settings.MigrationName}' created successfully in: {outputDir}");
                    return true;
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Failed to create migration with output directory specified.[/]");
                    AnsiConsole.MarkupLine("[yellow]Will try alternative approach...[/]");
                    return false;
                }
            });
        }

        /// <summary>
        /// Try to create migration with EF Core and then copy the files to our target location
        /// </summary>
        private async Task<bool> TryCreateAndCopyMigration(
            Settings settings,
            string contextName,
            string infrastructureProjectPath,
            string startupProjectPath,
            string targetDir,
            string provider,
            string connectionString)
        {
            return await AnsiConsole.Status().StartAsync("Creating migration and copying files...", async ctx =>
            {
                // Build EF Core command without output directory
                string efCoreCommand = $"migrations add {settings.MigrationName} " +
                                      $"--context {contextName} " +
                                      $"--project \"{infrastructureProjectPath}\" " +
                                      $"--startup-project \"{startupProjectPath}\" " +
                                      $"-- --provider {provider} --connection \"{connectionString}\"";

                ctx.Status($"Running: dotnet ef {efCoreCommand}");
                AnsiConsole.MarkupLine("[yellow]Executing command:[/]");
                AnsiConsole.WriteLine($"dotnet ef {efCoreCommand}");

                // Execute the command
                var result = await ExecuteCommand("dotnet", $"ef {efCoreCommand}", infrastructureProjectPath);

                if (result.ExitCode != 0)
                {
                    AnsiConsole.MarkupLine("[red]Failed to create migration.[/]");
                    AnsiConsole.MarkupLine("[red]Error output:[/]");
                    AnsiConsole.WriteLine(result.Error);
                    return false;
                }

                // Find and copy the migration files
                ctx.Status("Searching for migration files...");
                var migrationFiles = FindMigrationFiles(infrastructureProjectPath, settings.MigrationName);

                if (migrationFiles.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]No migration files found. Migration creation may have failed.[/]");
                    return false;
                }

                // Copy files to target directory
                ctx.Status("Copying migration files...");
                AnsiConsole.MarkupLine($"[green]Found {migrationFiles.Count} migration files:[/]");

                foreach (var file in migrationFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var targetPath = Path.Combine(targetDir, fileName);
                    File.Copy(file, targetPath, true);
                    AnsiConsole.MarkupLine($"[grey]- Copied: {fileName}[/]");
                }

                SafeMarkup.Success($"Migration '{settings.MigrationName}' created and copied to: {targetDir}");
                return true;
            });
        }

        /// <summary>
        /// Execute a command and capture its output
        /// </summary>
        private async Task<(int ExitCode, string Output, string Error)> ExecuteCommand(
            string fileName,
            string arguments,
            string workingDirectory)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            // Add environment variables for EF tools
            processStartInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
            processStartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return (process.ExitCode, output, error);
        }

        /// <summary>
        /// Find migration files generated by EF Core
        /// </summary>
        private List<string> FindMigrationFiles(string infrastructureProjectPath, string migrationName)
        {
            var result = new List<string>();
            var normalizedMigrationName = migrationName.Replace(" ", "");

            // Common locations where EF Core might put migration files
            var searchLocations = new List<string>
            {
                // Default EF Core location
                Path.Combine(infrastructureProjectPath, "Migrations"),
                
                // Common alternate locations
                Path.Combine(infrastructureProjectPath, "Data", "Migrations"),
                Path.Combine(infrastructureProjectPath, "Persistence", "Migrations"),
                Path.Combine(infrastructureProjectPath, "Database", "Migrations"),
                
                // Root project directory
                infrastructureProjectPath
            };

            foreach (var location in searchLocations)
            {
                if (Directory.Exists(location))
                {
                    // Look for all EF Core migration file types (.cs, .sql, etc.)
                    var files = Directory.GetFiles(location, $"*{normalizedMigrationName}*", SearchOption.AllDirectories);
                    result.AddRange(files);
                }
            }

            // If we still don't find anything, search the entire infrastructure project
            if (result.Count == 0)
            {
                var files = Directory.GetFiles(infrastructureProjectPath, $"*{normalizedMigrationName}*", SearchOption.AllDirectories);
                result.AddRange(files);
            }

            return result;
        }

        /// <summary>
        /// Find the solution directory starting from a given path
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

            return string.Empty;
        }

        /// <summary>
        /// Get the connection string for the specified tenant
        /// </summary>
        private string GetConnectionString(string tenantIdentifier, DbMigratorSettings settings)
        {
            // First check if tenant has specific connection string
            var tenant = settings.Tenants.Find(t => t.Identifier == tenantIdentifier);
            if (tenant != null && !string.IsNullOrEmpty(tenant.ConnectionString))
            {
                return tenant.ConnectionString;
            }

            // Otherwise use default connection string
            return settings.DefaultConnectionString;
        }
    }
}
