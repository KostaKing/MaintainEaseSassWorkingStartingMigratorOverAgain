using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;

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

                // Show status information
                await SpinnerComponents.WithSpinnerAsync(
                    "Checking database status...",
                    async () =>
                    {
                        // Simulate checking database status
                        await Task.Delay(1500);

                        // For demo, set some status properties
                        appContext.HasPendingMigrations = true;
                        appContext.PendingMigrationsCount = 3;
                        appContext.LastMigrationDate = DateTime.UtcNow.AddDays(-5);
                        appContext.LastMigrationName = "AddUserTable";
                    },
                    "Database");

                // Display status table
                // Use DatabaseInfoTable which already has Property and Value columns defined
                var table = TableComponents.CreateDatabaseInfoTable("Database Status");

                table.AddRow("Environment", $"[cyan]{SafeMarkup.EscapeMarkup(appContext.CurrentEnvironment)}[/]");
                table.AddRow("Provider", $"[cyan]{SafeMarkup.EscapeMarkup(appContext.CurrentProvider)}[/]");
                table.AddRow("Tenant", $"[cyan]{SafeMarkup.EscapeMarkup(appContext.CurrentTenant)}[/]");

                if (appContext.HasPendingMigrations)
                {
                    table.AddRow("Pending Migrations", $"[yellow]{appContext.PendingMigrationsCount}[/]");
                }
                else
                {
                    table.AddRow("Migrations", "[green]Up to date[/]");
                }

                if (appContext.LastMigrationDate.HasValue)
                {
                    table.AddRow("Last Migration", $"{SafeMarkup.EscapeMarkup(appContext.LastMigrationName)} " +
                        $"([grey]{appContext.LastMigrationDate:yyyy-MM-dd HH:mm}[/])");
                }

                AnsiConsole.WriteLine();
                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // If pending migrations, show what's pending
                if (appContext.HasPendingMigrations)
                {
                    SafeMarkup.Warning($"There are {appContext.PendingMigrationsCount} pending migrations.");
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
