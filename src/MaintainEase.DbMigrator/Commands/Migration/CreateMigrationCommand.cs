using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.Contracts.Interfaces;
using MaintainEase.DbMigrator.Plugins;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Commands.Migration
{
    /// <summary>
    /// Settings for the create migration command
    /// </summary>
    public class CreateMigrationCommandSettings : BaseCommandSettings
    {
        [CommandArgument(0, "[name]")]
        [Description("Name of the migration")]
        public string Name { get; set; }

        [CommandOption("--output")]
        [Description("Output directory for the migration files")]
        public string OutputDirectory { get; set; }

        [CommandOption("--provider")]
        [Description("Database provider (SqlServer or PostgreSQL)")]
        [DefaultValue("SqlServer")]
        public string Provider { get; set; } = "SqlServer";
    }



    /// <summary>
    /// Command to create a new database migration
    /// </summary>
    public class CreateMigrationCommand : AsyncCommand<CreateMigrationCommandSettings>
    {
        private readonly IServiceProvider _serviceProvider;

        public CreateMigrationCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override async Task<int> ExecuteAsync(CommandContext context, CreateMigrationCommandSettings settings)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var appContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<CreateMigrationCommand>>();
                var pluginService = scope.ServiceProvider.GetRequiredService<PluginService>();

                // Apply command settings to application context
                if (!string.IsNullOrEmpty(settings.Environment))
                {
                    appContext.CurrentEnvironment = settings.Environment;
                }

                if (!string.IsNullOrEmpty(settings.Tenant))
                {
                    await appContext.SwitchTenant(settings.Tenant);
                }

                // Display banner if not in batch mode
                if (!appContext.IsBatchMode)
                {
                    SafeMarkup.Banner("Create Migration", "blue");
                }

                // If no name provided, prompt for one
                var migrationName = settings.Name;
                if (string.IsNullOrEmpty(migrationName))
                {
                    migrationName = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter migration name:")
                            .ValidationErrorMessage("[red]Migration name cannot be empty[/]")
                            .Validate(name => string.IsNullOrWhiteSpace(name) 
                                ? ValidationResult.Error("Migration name cannot be empty") 
                                : ValidationResult.Success()));
                }

                // Get the connection string
                var connectionString = appContext.GetConnectionString(appContext.CurrentTenant);
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = AnsiConsole.Prompt(
                        new TextPrompt<string>($"Enter connection string for {settings.Provider}:")
                            .Secret()
                            .ValidationErrorMessage("[red]Connection string cannot be empty[/]")
                            .Validate(cs => string.IsNullOrWhiteSpace(cs) 
                                ? ValidationResult.Error("Connection string cannot be empty") 
                                : ValidationResult.Success()));
                }

                // Create request
                var request = new MigrationRequest
                {
                    ConnectionConfig = new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        ProviderName = settings.Provider
                    },
                    MigrationName = migrationName,
                    OutputDirectory = settings.OutputDirectory,
                    TenantId = appContext.CurrentTenant,
                    Environment = appContext.CurrentEnvironment,
                    Verbose = settings.Verbose
                };

                // Create migration
                var result = await SpinnerComponents.WithSpinnerAsync(
                    $"Creating migration '{migrationName}'...",
                    async () => await pluginService.CreateMigrationAsync(request),
                    "Processing");

                if (result.Success)
                {
                    SafeMarkup.Success($"Migration '{migrationName}' created successfully");
                    
                    if (result.AppliedMigrations.Count > 0)
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
                AnsiConsole.MarkupLine("[bold red]ERROR:[/] An unexpected error occurred");
                SafeMarkup.WriteException(ex);
                return 1;
            }
        }
    }
}
