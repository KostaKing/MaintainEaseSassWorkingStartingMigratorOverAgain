using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.Plugins;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;
using System.Collections.Generic;

namespace MaintainEase.DbMigrator.Commands.Migration
{
    /// <summary>
    /// Command to create a new migration
    /// </summary>
    public class CreateMigrationCommand : AsyncCommand<CreateMigrationCommand.Settings>
    {
        private readonly ILogger<CreateMigrationCommand> _logger;
        private readonly ApplicationContext _appContext;
        private readonly PluginService _pluginService;
        private readonly ConnectionManager _connectionManager;
        private readonly IOptions<DbMigratorSettings> _dbMigratorSettings;

        public CreateMigrationCommand(
            ILogger<CreateMigrationCommand> logger,
            ApplicationContext appContext,
            PluginService pluginService,
            ConnectionManager connectionManager,
            IOptions<DbMigratorSettings> dbMigratorSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _dbMigratorSettings = dbMigratorSettings ?? throw new ArgumentNullException(nameof(dbMigratorSettings));
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
                // Display info header
                SafeMarkup.Banner("Create Migration", "blue");
                SafeMarkup.Info($"Creating migration: {settings.MigrationName}");
                SafeMarkup.Info($"Tenant: {settings.TenantIdentifier}");

                // If no provider is specified, use the current one from application context
                string provider = settings.Provider ?? _appContext.CurrentProvider;
                SafeMarkup.Info($"Provider: {provider}");

                // Determine migration output directory
                string outputDirectory = DetermineMigrationOutputDirectory(settings, provider);
                SafeMarkup.Info($"Migration output directory: {outputDirectory}");

                // Ensure directory exists
                Directory.CreateDirectory(outputDirectory);

                // Get connection string from connection manager
                string connectionString = _connectionManager.GetConnectionString(provider);
                _logger.LogDebug("Using connection string: {ConnectionString}", MaskConnectionString(connectionString));

                // Create migration request for the plugin with additional context information
                var migrationRequest = new MigrationRequest
                {
                    MigrationName = settings.MigrationName,
                    OutputDirectory = outputDirectory,
                    ConnectionConfig = new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        ProviderName = provider,
                        Timeout = _dbMigratorSettings.Value.Providers?.SqlServer?.CommandTimeout ?? 30,
                        UseTransaction = true
                    },
                    TenantId = settings.TenantIdentifier,
                    Environment = _appContext.CurrentEnvironment,
                    Verbose = _appContext.IsDebugMode,
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        { "DbContext", settings.DbContext.EndsWith("DbContext") ? settings.DbContext : $"{settings.DbContext}DbContext" },
                        { "SolutionDir", _appContext.WorkingDirectory },
                        { "StartupProject", Path.Combine(_appContext.WorkingDirectory, "src", "MaintainEase.DbMigrator") }
                    }
                };

                // Create the migration using the plugin service with spinner
                var result = await SpinnerComponents.WithSpinnerAsync(
                    $"Creating migration '{settings.MigrationName}'...",
                    async () => await _pluginService.CreateMigrationAsync(migrationRequest),
                    "Migration");

                if (result.Success)
                {
                    SafeMarkup.Success($"Migration '{settings.MigrationName}' created successfully!");

                    if (result.AppliedMigrations?.Count > 0)
                    {
                        // Display information about the created migration files
                        AnsiConsole.MarkupLine($"[green]Created the following migration files:[/]");
                        var table = TableComponents.CreateTable("Id", "Name", "Path");

                        foreach (var migration in result.AppliedMigrations)
                        {
                            table.AddRow(
                                migration.Id ?? "-",
                                SafeMarkup.EscapeMarkup(migration.Name ?? "Unknown"),
                                migration.Script != null ? SafeMarkup.EscapeMarkup(Path.GetFileName(migration.Script)) : "-"
                            );
                        }

                        AnsiConsole.Write(table);
                        AnsiConsole.WriteLine();

                        // If additional information is available, display it
                        if (result.AdditionalInfo != null && result.AdditionalInfo.Count > 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]Additional information:[/]");
                            foreach (var info in result.AdditionalInfo)
                            {
                                // Only display non-sensitive information
                                if (!info.Key.Contains("password", StringComparison.OrdinalIgnoreCase) &&
                                    !info.Key.Contains("secret", StringComparison.OrdinalIgnoreCase))
                                {
                                    AnsiConsole.MarkupLine($"[grey]{info.Key}:[/] {SafeMarkup.EscapeMarkup(info.Value)}");
                                }
                            }
                        }
                    }

                    return 0;
                }
                else
                {
                    SafeMarkup.Error($"Failed to create migration: {result.ErrorMessage}");

                    // Display any additional error information if available
                    if (result.AdditionalInfo != null &&
                        result.AdditionalInfo.TryGetValue("CommandOutput", out var commandOutput))
                    {
                        AnsiConsole.MarkupLine("[yellow]Command output:[/]");
                        AnsiConsole.WriteLine(commandOutput);
                    }

                    return 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating migration");
                SafeMarkup.Error("An unexpected error occurred");
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }

        /// <summary>
        /// Determine the output directory for the migration files
        /// </summary>
        private string DetermineMigrationOutputDirectory(Settings settings, string provider)
        {
            if (!string.IsNullOrEmpty(settings.OutputDir))
            {
                // Use explicit output directory if provided
                return Path.GetFullPath(settings.OutputDir);
            }
            else
            {
                // Use organized structure: Migrations/[Provider]/[Tenant]
                string basePath = _appContext.MigrationsDirectory;

                // Create path with provider and tenant folders
                return Path.Combine(basePath, provider, settings.TenantIdentifier);
            }
        }

        /// <summary>
        /// Mask sensitive information in connection string for logging
        /// </summary>
        private string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "[empty]";

            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|pwd)=([^;]*)",
                "$1=********",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
