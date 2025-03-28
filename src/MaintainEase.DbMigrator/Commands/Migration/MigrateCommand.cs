using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;

namespace MaintainEase.DbMigrator.Commands.Migration
{
    /// <summary>
    /// Settings for the migrate command
    /// </summary>
    public class MigrateCommandSettings : BaseCommandSettings
    {
        [CommandOption("--backup")]
        [Description("Create a backup before applying migrations")]
        [DefaultValue(true)]
        public bool CreateBackup { get; set; } = true;

        [CommandOption("--script")]
        [Description("Generate SQL scripts without applying migrations")]
        public bool ScriptOnly { get; set; }

        [CommandOption("--output")]
        [Description("Path for generated SQL scripts")]
        public string? OutputPath { get; set; }
    }

    /// <summary>
    /// Command to apply database migrations
    /// </summary>
    public class MigrateCommand : AsyncCommand<MigrateCommandSettings>
    {
        private readonly IServiceProvider _serviceProvider;

        public MigrateCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override async Task<int> ExecuteAsync(CommandContext context, MigrateCommandSettings settings)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var appContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<MigrateCommand>>();

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
                    SafeMarkup.Banner("Database Migration", "green");
                }

                logger.LogInformation("Running migrations for tenant {Tenant} in {Environment} environment",
                    appContext.CurrentTenant, appContext.CurrentEnvironment);

                // Check if there are pending migrations
                await SpinnerComponents.WithSpinnerAsync(
                    "Checking for pending migrations...",
                    async () =>
                    {
                        // Simulate checking for migrations
                        await Task.Delay(1000);

                        // For demo, set some status properties
                        appContext.HasPendingMigrations = true;
                        appContext.PendingMigrationsCount = 3;
                    },
                    "Database");

                if (!appContext.HasPendingMigrations)
                {
                    SafeMarkup.Success("Database is already up to date. No migrations to apply.");
                    return 0;
                }

                SafeMarkup.Info($"Found {appContext.PendingMigrationsCount} pending migrations.");

                // Create backup if requested
                if (settings.CreateBackup)
                {
                    var backupPath = await CreateBackupAsync();
                    SafeMarkup.Success($"Database backup created at: {backupPath}");
                }

                // Run or script migrations
                if (settings.ScriptOnly)
                {
                    await GenerateScriptsAsync(settings.OutputPath ?? "./Scripts");
                }
                else
                {
                    bool confirmed = settings.NoPrompt || DialogComponents.ShowConfirmation(
                        "Apply Migrations",
                        $"Are you sure you want to apply {appContext.PendingMigrationsCount} migrations to the {appContext.CurrentTenant} database?",
                        false);

                    if (confirmed)
                    {
                        await ApplyMigrationsAsync();
                    }
                    else
                    {
                        SafeMarkup.Warning("Migration operation cancelled by user.");
                        return 0;
                    }
                }

                return 0; // Successful execution
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[bold red]ERROR:[/] An unexpected error occurred");
                SafeMarkup.WriteException(ex);
                return 1; // Error
            }
        }

        private async Task<string> CreateBackupAsync()
        {
            string backupPath = "";

            // Fixed: Changed the delegate to use a proper function
            await SpinnerComponents.WithStatusSpinnerAsync(
                "Creating database backup...",
                async (ctx) =>
                {
                    // Simulate backup creation
                    await Task.Delay(2000);
                    backupPath = $"./Backups/backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                    ctx.Status($"Backup created: {backupPath}");
                },
                "Database");

            return backupPath;
        }

        private async Task GenerateScriptsAsync(string outputPath)
        {
            await ProgressComponents.ProcessWithProgressAsync(
                new[] { "Schema changes", "Data migrations", "Indexes", "Constraints" },
                async item =>
                {
                    // Simulate script generation
                    await Task.Delay(1000);
                },
                "Generating migration scripts...");

            SafeMarkup.Success($"Migration scripts generated in: {outputPath}");
        }

        private async Task ApplyMigrationsAsync()
        {
            var progress = AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn()
                });

            await progress.StartAsync(async ctx =>
            {
                var migrationTask = ctx.AddTask("Applying migrations", maxValue: 100);

                // Simulate applying migrations with progress updates
                for (int i = 0; i < 100; i += 5)
                {
                    migrationTask.Value = i;
                    await Task.Delay(100);
                }

                migrationTask.Value = 100;
            });

            // Update application context to reflect applied migrations
            using (var scope = _serviceProvider.CreateScope())
            {
                var appContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                appContext.HasPendingMigrations = false;
                appContext.PendingMigrationsCount = 0;
                appContext.LastMigrationDate = DateTime.UtcNow;
                appContext.LastMigrationName = "CompleteMigration";
            }

            SafeMarkup.Success("All migrations were successfully applied.");
        }
    }
}
