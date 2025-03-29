using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.Plugins;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

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

        public CreateMigrationCommand(
            ILogger<CreateMigrationCommand> logger,
            ApplicationContext appContext,
            PluginService pluginService,
            ConnectionManager connectionManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
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

            [CommandArgument(0, "<n>")]
            [Description("Name of the migration")]
            public string MigrationName { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            try
            {
                // If no provider is specified, use the current one from application context
                string provider = settings.Provider ?? _appContext.CurrentProvider;

                // Display info header
                SafeMarkup.Banner("Create Migration", "blue");
                SafeMarkup.Info($"Creating migration: {settings.MigrationName}");
                SafeMarkup.Info($"Tenant: {settings.TenantIdentifier}");
                SafeMarkup.Info($"Provider: {provider}");

                // Determine migration output directory
                string outputDirectory = DetermineMigrationOutputDirectory(settings, provider);
                SafeMarkup.Info($"Migration output directory: {outputDirectory}");

                // Ensure directory exists
                Directory.CreateDirectory(outputDirectory);

                // Get connection string from connection manager
                string connectionString = _connectionManager.GetConnectionString(provider);
                _logger.LogDebug("Using connection string: {ConnectionString}", MaskConnectionString(connectionString));

                // Create migration request for the plugin
                var migrationRequest = new MigrationRequest
                {
                    MigrationName = settings.MigrationName,
                    OutputDirectory = outputDirectory,
                    ConnectionConfig = new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        ProviderName = provider
                    },
                    TenantId = settings.TenantIdentifier,
                    Environment = _appContext.CurrentEnvironment,
                    Verbose = _appContext.IsDebugMode
                };

                // Create the migration using the plugin service
                var result = await SpinnerComponents.WithSpinnerAsync(
                    $"Creating migration '{settings.MigrationName}'...",
                    async () => await _pluginService.CreateMigrationAsync(migrationRequest),
                    "Migration");

                if (result.Success)
                {
                    SafeMarkup.Success($"Migration '{settings.MigrationName}' created successfully!");

                    if (result.AppliedMigrations?.Count > 0)
                    {
                        var migration = result.AppliedMigrations[0];
                        SafeMarkup.Info($"Migration ID: {migration.Id}");

                        if (!string.IsNullOrEmpty(migration.Script))
                        {
                            SafeMarkup.Info($"Script: {migration.Script}");
                        }
                    }

                    return 0;
                }
                else
                {
                    SafeMarkup.Error($"Failed to create migration: {result.ErrorMessage}");
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
