using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.Commands.Migration;

namespace MaintainEase.DbMigrator.Commands.Database
{
    /// <summary>
    /// Command to show database migration status
    /// </summary>
    public class StatusCommand : AsyncCommand<BaseCommandSettings>
    {
        private readonly IServiceProvider _serviceProvider;

        public StatusCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override async Task<int> ExecuteAsync(CommandContext context, BaseCommandSettings settings)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var appContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<StatusCommand>>();
                var migrationHelper = scope.ServiceProvider.GetRequiredService<MigrationHelper>();

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
                    SafeMarkup.Banner("Database Status", "blue");
                }

                logger.LogInformation("Checking database status for tenant {Tenant} in {Environment} environment",
                    appContext.CurrentTenant, appContext.CurrentEnvironment);

                // Check database status
                var status = await SpinnerComponents.WithSpinnerAsync(
                    "Checking database status...",
                    async () => await migrationHelper.CheckMigrationStatusAsync(),
                    "Database");

                // Display status table
                var table = TableComponents.CreateDatabaseInfoTable("Database Status");

                table.AddRow("Environment", $"[cyan]{SafeMarkup.EscapeMarkup(appContext.CurrentEnvironment)}[/]");
                table.AddRow("Provider", $"[cyan]{SafeMarkup.EscapeMarkup(appContext.CurrentProvider)}[/]");
                table.AddRow("Tenant", $"[cyan]{SafeMarkup.EscapeMarkup(appContext.CurrentTenant)}[/]");

                if (!string.IsNullOrEmpty(status.DatabaseName))
                {
                    table.AddRow("Database", SafeMarkup.EscapeMarkup(status.DatabaseName));
                }

                if (!string.IsNullOrEmpty(status.DatabaseVersion))
                {
                    table.AddRow("Version", SafeMarkup.EscapeMarkup(status.DatabaseVersion));
                }

                if (status.HasPendingMigrations)
                {
                    table.AddRow("Pending Migrations", $"[yellow]{status.PendingMigrationsCount}[/]");
                }
                else
                {
                    table.AddRow("Migrations", "[green]Up to date[/]");
                }

                if (status.LastMigrationDate.HasValue)
                {
                    table.AddRow("Last Migration", $"{SafeMarkup.EscapeMarkup(status.LastMigrationName)} " +
                        $"([grey]{status.LastMigrationDate:yyyy-MM-dd HH:mm}[/])");
                }

                AnsiConsole.WriteLine();
                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // If pending migrations, show what's pending
                if (status.HasPendingMigrations && status.PendingMigrations?.Count > 0)
                {
                    // Display pending migrations
                    var migrationsTable = TableComponents.CreateMigrationTable("Pending Migrations");

                    foreach (var migration in status.PendingMigrations)
                    {
                        migrationsTable.AddRow(
                            migration.Id ?? "-",
                            SafeMarkup.EscapeMarkup(migration.Name ?? "Unknown"),
                            migration.Created?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                            "[yellow]Pending[/]",
                            "-"
                        );
                    }

                    AnsiConsole.Write(migrationsTable);
                    AnsiConsole.WriteLine();

                    SafeMarkup.Warning($"There are {status.PendingMigrationsCount} pending migrations.");
                    SafeMarkup.Info("Run the 'migrate' command to apply pending migrations.");
                }
                else if (status.HasPendingMigrations)
                {
                    SafeMarkup.Warning($"There are {status.PendingMigrationsCount} pending migrations.");
                    SafeMarkup.Info("Run the 'migrate' command to apply pending migrations.");
                }
                else
                {
                    SafeMarkup.Success("Database is up to date.");
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
    }
}
