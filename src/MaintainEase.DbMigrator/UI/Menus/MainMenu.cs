using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.UI.Dialogs;
using MaintainEase.DbMigrator.UI.Theme;
using MaintainEase.DbMigrator.Commands.Database;
using MaintainEase.DbMigrator.Commands.Migration;
using MaintainEase.DbMigrator.Commands;
using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MaintainEase.DbMigrator.UI.Menus
{
    /// <summary>
    /// Main menu for the interactive console application
    /// </summary>
    public class MainMenu
    {
        private readonly ApplicationContext _appContext;
        private readonly ILogger _logger;
        private readonly CommandContext _commandContext;

        public MainMenu(ApplicationContext appContext, ILogger logger)
        {
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize command context with minimal command info
            // Create a proper remaining arguments implementation
            var remaining = new EmptyRemainingArguments();

            _commandContext = new CommandContext(
                Array.Empty<string>(),   // Empty command arguments
                remaining,               // Properly implemented remaining arguments
                "interactive",           // Command name for reference
                null);                   // No parsed result
        }

        // Proper implementation of IRemainingArguments with correct return types
        private class EmptyRemainingArguments : IRemainingArguments
        {
            // Parsed returns an empty lookup
            public ILookup<string, string?> Parsed =>
                Array.Empty<KeyValuePair<string, string?>>()
                    .ToLookup(x => x.Key, x => x.Value);

            // Raw returns an empty read-only list
            public IReadOnlyList<string> Raw => Array.Empty<string>();
        }

        /// <summary>
        /// Run the main menu
        /// </summary>
        public async Task RunAsync()
        {
            bool exit = false;

            while (!exit)
            {
                Console.Clear();
                ShowBanner();
                ShowStatus();

                string choice = ShowMainMenuOptions();
                exit = await HandleMenuChoiceAsync(choice);
            }
        }

        /// <summary>
        /// Show application banner
        /// </summary>
        private void ShowBanner()
        {
            SafeMarkup.Banner($"MaintainEase DB Migrator v{_appContext.Version}", "blue");
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Show current status information
        /// </summary>
        private void ShowStatus()
        {
            // Use DatabaseInfoTable which is pre-configured with "Property" and "Value" columns
            var table = TableComponents.CreateDatabaseInfoTable("Application Status");

            table.AddRow("Environment", $"[cyan]{SafeMarkup.EscapeMarkup(_appContext.CurrentEnvironment)}[/]");
            table.AddRow("Database Provider", $"[cyan]{SafeMarkup.EscapeMarkup(_appContext.CurrentProvider)}[/]");
            table.AddRow("Current Tenant", $"[cyan]{SafeMarkup.EscapeMarkup(_appContext.CurrentTenant)}[/]");

            if (_appContext.HasPendingMigrations)
            {
                table.AddRow("Migrations", $"[yellow]{_appContext.PendingMigrationsCount} pending[/]");
            }
            else
            {
                table.AddRow("Migrations", "[green]Up to date[/]");
            }

            table.AddRow("Mode", _appContext.IsDebugMode ? "[yellow]Debug[/]" : "[blue]Release[/]");

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Show the main menu options
        /// </summary>
        private string ShowMainMenuOptions()
        {
            var menuOptions = new Dictionary<string, string>
            {
                ["1"] = "Database Operations",
                ["2"] = "Manage Database Migrations",
                ["3"] = "Environment Management",
                ["4"] = "Tenant Management",
                ["5"] = "Visualization & Reports",
                ["6"] = "Settings",
                ["7"] = "Help & Documentation",
                ["0"] = "Exit"
            };

            return MenuComponents.ShowMainMenu("Main Menu", menuOptions);
        }

        /// <summary>
        /// Handle selection from the main menu
        /// </summary>
        private async Task<bool> HandleMenuChoiceAsync(string choice)
        {
            // Extract the menu key (the number before the dash)
            string key = choice.Split(' ')[0];

            switch (key)
            {
                case "1": // Database Operations
                    await ShowDatabaseOperationsMenuAsync();
                    break;
                case "2": // Manage Database Migrations
                    await ShowMigrationMenuAsync();
                    break;
                case "3": // Environment Management
                    await ShowEnvironmentMenuAsync();
                    break;
                case "4": // Tenant Management
                    await ShowTenantMenuAsync();
                    break;
                case "5": // Visualization & Reports
                    await ShowVisualizationMenuAsync();
                    break;
                case "6": // Settings
                    await ShowSettingsMenuAsync();
                    break;
                case "7": // Help & Documentation
                    ShowHelpDialog();
                    break;
                case "0": // Exit
                    return await ConfirmExitAsync();
                default:
                    SafeMarkup.Warning("Invalid option selected.");
                    PressEnterToContinue();
                    break;
            }

            return false;
        }

        /// <summary>
        /// Show the Database Operations menu
        /// </summary>
        private async Task ShowDatabaseOperationsMenuAsync()
        {
            await SpinnerComponents.WithSpinnerAsync(
                "Loading database operations...",
                async () => await Task.Delay(500),
                "Database");

            SafeMarkup.SectionHeader("Database Operations");

            var options = new Dictionary<string, string>
            {
                ["1"] = "View Database Status",
                ["2"] = "Execute SQL Query",
                ["3"] = "Backup Database",
                ["4"] = "Restore Database",
                ["5"] = "Configure Connection",
                ["0"] = "Back to Main Menu"
            };

            string choice = MenuComponents.ShowMainMenu("Database Operations", options);
            string key = choice.Split(' ')[0];

            switch (key)
            {
                case "1": // View Database Status
                    // Execute the status command directly
                    await ExecuteStatusCommandAsync();
                    break;
                case "2":
                case "3":
                case "4":
                case "5":
                    DialogComponents.ShowInfo("Feature Coming Soon",
                        "This database operation feature will be implemented in a future update.");
                    break;
            }

            if (key != "0") PressEnterToContinue();
        }

        /// <summary>
        /// Execute the status command
        /// </summary>
        private async Task ExecuteStatusCommandAsync()
        {
            try
            {
                var settings = new BaseCommandSettings
                {
                    Tenant = _appContext.CurrentTenant,
                    Environment = _appContext.CurrentEnvironment,
                    Verbose = _appContext.IsDebugMode
                };

                // Use the service provider from the application context
                // instead of passing null to the constructor
                using (var scope = Program.ServiceProvider.CreateScope())
                {
                    var serviceProvider = scope.ServiceProvider;
                    var statusCommand = new StatusCommand(serviceProvider);
                    await statusCommand.ExecuteAsync(_commandContext, settings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing status command");
                DialogComponents.ShowException(ex, "Command Error");
            }
        }

        /// <summary>
        /// Show the Migration Management menu
        /// </summary>
        private async Task ShowMigrationMenuAsync()
        {
            await SpinnerComponents.WithSpinnerAsync(
                "Loading migration management...",
                async () => await Task.Delay(500),
                "Migration");

            SafeMarkup.SectionHeader("Migration Management");

            var options = new Dictionary<string, string>
            {
                ["1"] = "Run Migrations",
                ["2"] = "View Migration History",
                ["3"] = "Create New Migration",
                ["4"] = "Generate Migration Script",
                ["5"] = "Rollback Migration",
                ["0"] = "Back to Main Menu"
            };

            string choice = MenuComponents.ShowMainMenu("Migration Management", options);
            string key = choice.Split(' ')[0];

            switch (key)
            {
                case "1": // Run Migrations
                    await ExecuteMigrateCommandAsync();
                    break;
                case "2":
                case "3":
                case "4":
                case "5":
                    DialogComponents.ShowInfo("Feature Coming Soon",
                        "This migration management feature will be implemented in a future update.");
                    break;
            }

            if (key != "0") PressEnterToContinue();
        }

        /// <summary>
        /// Execute the migrate command
        /// </summary>
        private async Task ExecuteMigrateCommandAsync()
        {
            try
            {
                var settings = new MigrateCommandSettings
                {
                    Tenant = _appContext.CurrentTenant,
                    Environment = _appContext.CurrentEnvironment,
                    Verbose = _appContext.IsDebugMode,
                    CreateBackup = _appContext.AutoBackupBeforeMigration,
                    NoPrompt = false // Always prompt in interactive mode
                };

                // Use the service provider from Program class
                using (var scope = Program.ServiceProvider.CreateScope())
                {
                    var serviceProvider = scope.ServiceProvider;
                    var migrateCommand = new MigrateCommand(serviceProvider);
                    await migrateCommand.ExecuteAsync(_commandContext, settings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing migrate command");
                DialogComponents.ShowException(ex, "Command Error");
            }
        }

        /// <summary>
        /// Show the Environment Management menu
        /// </summary>
        private async Task ShowEnvironmentMenuAsync()
        {
            await SpinnerComponents.WithSpinnerAsync(
                "Loading environment management...",
                async () => await Task.Delay(500),
                "Processing");

            SafeMarkup.SectionHeader("Environment Management");

            var options = new Dictionary<string, string>
            {
                ["1"] = "Switch Environment",
                ["2"] = "Configure Environment",
                ["3"] = "Compare Environments",
                ["4"] = "Sync Environments",
                ["0"] = "Back to Main Menu"
            };

            string choice = MenuComponents.ShowMainMenu("Environment Management", options);
            string key = choice.Split(' ')[0];

            if (key == "1")
            {
                await SwitchEnvironmentAsync();
            }
            else if (key != "0")
            {
                DialogComponents.ShowInfo("Feature Coming Soon",
                    "This environment management feature will be implemented in a future update.");
                PressEnterToContinue();
            }
        }

        /// <summary>
        /// Switch to a different environment
        /// </summary>
        private async Task SwitchEnvironmentAsync()
        {
            var environments = new[] { "Development", "Testing", "Staging", "Production" };

            var selectedEnvironment = AnsiConsole.Prompt(
                MenuComponents.CreateSelectionMenu(
                    "Select Environment:",
                    environments,
                    env => env == _appContext.CurrentEnvironment ? $"{env} (Current)" : env));

            if (selectedEnvironment != _appContext.CurrentEnvironment)
            {
                // Confirm if switching to Production
                if (selectedEnvironment == "Production" &&
                    !DialogComponents.ShowConfirmation(
                        "Production Environment",
                        "Are you sure you want to switch to the Production environment? This should only be done for actual production deployments.",
                        false))
                {
                    SafeMarkup.Warning("Environment switch cancelled.");
                    PressEnterToContinue();
                    return;
                }

                await SpinnerComponents.WithSpinnerAsync(
                    $"Switching to {selectedEnvironment} environment...",
                    async () =>
                    {
                        _appContext.CurrentEnvironment = selectedEnvironment;
                        await Task.Delay(1000); // Simulated work
                    });

                DialogComponents.ShowSuccess("Environment Switched", $"Successfully switched to {selectedEnvironment} environment.");
            }
            else
            {
                SafeMarkup.Info($"Already using {selectedEnvironment} environment.");
            }

            PressEnterToContinue();
        }

        /// <summary>
        /// Show the Tenant Management menu
        /// </summary>
        private async Task ShowTenantMenuAsync()
        {
            await SpinnerComponents.WithSpinnerAsync(
                "Loading tenant management...",
                async () => await Task.Delay(500));

            SafeMarkup.SectionHeader("Tenant Management");

            var options = new Dictionary<string, string>
            {
                ["1"] = "Switch Tenant",
                ["2"] = "Add New Tenant",
                ["3"] = "Remove Tenant",
                ["4"] = "Sync Tenants",
                ["0"] = "Back to Main Menu"
            };

            string choice = MenuComponents.ShowMainMenu("Tenant Management", options);
            string key = choice.Split(' ')[0];

            if (key == "1")
            {
                await SwitchTenantAsync();
            }
            else if (key != "0")
            {
                DialogComponents.ShowInfo("Feature Coming Soon",
                    "This tenant management feature will be implemented in a future update.");
                PressEnterToContinue();
            }
        }

        /// <summary>
        /// Switch to a different tenant
        /// </summary>
        private async Task SwitchTenantAsync()
        {
            // Use available tenants or create some demo tenants if none available
            var tenants = _appContext.AvailableTenants.Count > 0
                ? _appContext.AvailableTenants
                : new List<string> { "Default", "Tenant1", "Tenant2", "Tenant3" };

            var selectedTenant = AnsiConsole.Prompt(
                MenuComponents.CreateSelectionMenu(
                    "Select Tenant:",
                    tenants,
                    t => t == _appContext.CurrentTenant ? $"{t} (Current)" : t));

            if (selectedTenant != _appContext.CurrentTenant)
            {
                await SpinnerComponents.WithSpinnerAsync(
                    $"Switching to {selectedTenant} tenant...",
                    async () =>
                    {
                        // Add the tenant if it doesn't exist (for demo purposes)
                        if (!_appContext.TenantExists(selectedTenant))
                        {
                            _appContext.AddTenant(selectedTenant);
                        }

                        await _appContext.SwitchTenant(selectedTenant);
                    });

                DialogComponents.ShowSuccess("Tenant Switched", $"Successfully switched to {selectedTenant} tenant.");
            }
            else
            {
                SafeMarkup.Info($"Already using {selectedTenant} tenant.");
            }

            PressEnterToContinue();
        }

        /// <summary>
        /// Show the Visualization & Reports menu
        /// </summary>
        private async Task ShowVisualizationMenuAsync()
        {
            await SpinnerComponents.WithSpinnerAsync(
                "Loading visualization tools...",
                async () => await Task.Delay(500),
                "Analyzing");

            SafeMarkup.SectionHeader("Visualization & Reports");

            // Show a demo chart
            ShowDemoVisualizations();

            var options = new Dictionary<string, string>
            {
                ["1"] = "Database Growth Chart",
                ["2"] = "Migration Timeline",
                ["3"] = "Schema Comparison",
                ["4"] = "Performance Dashboard",
                ["0"] = "Back to Main Menu"
            };

            string choice = MenuComponents.ShowMainMenu("Visualization & Reports", options);
            string key = choice.Split(' ')[0];

            if (key != "0")
            {
                DialogComponents.ShowInfo("Feature Coming Soon",
                    "This visualization feature will be implemented in a future update.");
                PressEnterToContinue();
            }
        }

        /// <summary>
        /// Show some demo visualizations
        /// </summary>
        private void ShowDemoVisualizations()
        {
            // Create a demo bar chart for database operations
            var chart = ChartComponents.CreateBarChart("Recent Database Operations");
            chart.AddBarChartCategory("Migrations", 45);
            chart.AddBarChartCategory("Backups", 32);
            chart.AddBarChartCategory("Restores", 12);
            chart.AddBarChartCategory("Queries", 87);
            chart.AddBarChartCategory("Schema Changes", 23);

            AnsiConsole.Write(chart);
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Show the Settings menu
        /// </summary>
        private async Task ShowSettingsMenuAsync()
        {
            await SpinnerComponents.WithSpinnerAsync(
                "Loading settings...",
                async () => await Task.Delay(500));

            SafeMarkup.SectionHeader("Settings");

            var options = new Dictionary<string, string>
            {
                ["1"] = "Change Theme",
                ["2"] = "Configure Paths",
                ["3"] = "User Preferences",
                ["4"] = "Connection Settings",
                ["5"] = "About Application",
                ["0"] = "Back to Main Menu"
            };

            string choice = MenuComponents.ShowMainMenu("Settings", options);
            string key = choice.Split(' ')[0];

            switch (key)
            {
                case "1":
                    await ChangeThemeAsync();
                    break;
                case "2":
                case "3":
                case "4":
                    DialogComponents.ShowInfo("Feature Coming Soon",
                        "This settings feature will be implemented in a future update.");
                    PressEnterToContinue();
                    break;
                case "5":
                    ShowAboutDialog();
                    PressEnterToContinue();
                    break;
            }
        }

        /// <summary>
        /// Show the Help dialog
        /// </summary>
        private void ShowHelpDialog()
        {
            var helpDialog = new HelpDialog();
            helpDialog.Show();
        }

        /// <summary>
        /// Show information about the application
        /// </summary>
        private void ShowAboutDialog()
        {
            var panel = new Panel(
                new Markup($"[bold]MaintainEase DB Migrator[/]\\n\\n" +
                          $"Version: {_appContext.Version}\\n" +
                          $"Project: {_appContext.ProjectName}\\n\\n" +
                          $"A powerful database migration tool designed to make\\n" +
                          $"database schema management easy and reliable across\\n" +
                          $"development, testing, and production environments.\\n\\n" +
                          $"[grey]Copyright © {DateTime.Now.Year} MaintainEase Team[/]\\n" +
                          $"[grey]All rights reserved.[/]"))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(2),
                Header = new PanelHeader("[blue]About[/]")
            };

            AnsiConsole.Write(panel);
        }

        /// <summary>
        /// Change the application theme
        /// </summary>
        private async Task ChangeThemeAsync()
        {
            var themes = ThemeManager.GetAvailableThemes();
            string currentTheme = ThemeManager.GetCurrentTheme();

            var selectedTheme = AnsiConsole.Prompt(
                MenuComponents.CreateSelectionMenu(
                    "Select Theme:",
                    themes,
                    t => t == currentTheme ? $"{t} (Current)" : t));

            if (selectedTheme != currentTheme)
            {
                await SpinnerComponents.WithSpinnerAsync(
                    $"Changing theme to {selectedTheme}...",
                    async () =>
                    {
                        ThemeManager.SetTheme(selectedTheme);
                        _appContext.PreferredTheme = selectedTheme;
                        await Task.Delay(500); // Visual delay for feedback
                    });

                DialogComponents.ShowSuccess("Theme Changed", $"Theme changed to {selectedTheme}.");
            }
            else
            {
                SafeMarkup.Info($"Already using {selectedTheme} theme.");
            }

            PressEnterToContinue();
        }

        /// <summary>
        /// Confirm before exiting the application
        /// </summary>
        private async Task<bool> ConfirmExitAsync()
        {
            if (DialogComponents.ShowConfirmation(
                    "Exit Confirmation",
                    "Are you sure you want to exit the application?",
                    false))
            {
                await SpinnerComponents.WithSpinnerAsync(
                    "Saving application state...",
                    async () =>
                    {
                        _appContext.SaveSettings();
                        await Task.Delay(500);
                    });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Utility method for "Press Enter to continue" prompt
        /// </summary>
        private void PressEnterToContinue()
        {
            AnsiConsole.WriteLine();
            // Use SafeMarkup to properly escape the brackets
            AnsiConsole.Markup("Press [grey][[Enter]][/] to continue...");
            Console.ReadLine(); // Simple ReadLine instead of Prompt
        }
    }
}
