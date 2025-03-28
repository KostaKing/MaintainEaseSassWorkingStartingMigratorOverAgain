using System;
using System.Threading.Tasks;
using Spectre.Console;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.Commands.Migration;

namespace MaintainEase.DbMigrator.UI.Menus
{
    /// <summary>
    /// Menu for managing database providers
    /// </summary>
    public class DatabaseProvidersMenu
    {
        private readonly ApplicationContext _appContext;
        private readonly ConnectionManager _connectionManager;
        private readonly MigrationHelper _migrationHelper;

        public DatabaseProvidersMenu(
            ApplicationContext appContext,
            ConnectionManager connectionManager,
            MigrationHelper migrationHelper)
        {
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _migrationHelper = migrationHelper ?? throw new ArgumentNullException(nameof(migrationHelper));
        }

        /// <summary>
        /// Display the database providers menu
        /// </summary>
        public async Task ShowAsync()
        {
            bool exit = false;

            while (!exit)
            {
                // Clear screen and show header
                AnsiConsole.Clear();
                SafeMarkup.SectionHeader("Database Providers", color: "blue");
                AnsiConsole.WriteLine();

                // Show current provider
                SafeMarkup.Info($"Current provider: {_appContext.CurrentProvider}");
                AnsiConsole.WriteLine();

                // Display available providers
                var providers = _connectionManager.GetAvailableProviders();
                var table = TableComponents.CreateTable("Available Providers");

                foreach (var provider in providers)
                {
                    var isActive = provider == _appContext.CurrentProvider;
                    table.AddRow(
                        provider,
                        isActive ? "[green]✓ Active[/]" : "[grey]Inactive[/]"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Show menu options
                var choice = MenuComponents.ShowMainMenu("Database Provider Options", new Dictionary<string, string>
                {
                    ["1"] = "Switch Provider",
                    ["2"] = "Test Connection",
                    ["3"] = "Configure Connection String",
                    ["0"] = "Back to Database Menu"
                });

                // Handle menu choice
                switch (choice.Split(' ')[0])
                {
                    case "1":
                        await SwitchProvider();
                        break;
                    case "2":
                        await TestConnection();
                        break;
                    case "3":
                        await ConfigureConnectionString();
                        break;
                    case "0":
                        exit = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Switch database provider
        /// </summary>
        private async Task SwitchProvider()
        {
            var providers = _connectionManager.GetAvailableProviders();

            if (providers.Count == 0)
            {
                SafeMarkup.Warning("No providers available. Check your configuration.");
                PressEnterToContinue();
                return;
            }

            var selectedProvider = AnsiConsole.Prompt(
                MenuComponents.CreateSelectionMenu(
                    "Select Database Provider:",
                    providers,
                    p => p == _appContext.CurrentProvider ? $"{p} (Current)" : p));

            if (selectedProvider != _appContext.CurrentProvider)
            {
                await SpinnerComponents.WithSpinnerAsync(
                    $"Switching to {selectedProvider} provider...",
                    async () =>
                    {
                        // Switch provider
                        var success = _connectionManager.SwitchProvider(selectedProvider);

                        if (success)
                        {
                            // Update application context
                            _appContext.CurrentProvider = selectedProvider;

                            // Check migration status with new provider
                            await _migrationHelper.CheckMigrationStatusAsync();
                        }
                    },
                    "Database");

                SafeMarkup.Success($"Provider switched to {selectedProvider}");
            }
            else
            {
                SafeMarkup.Info($"Already using {selectedProvider} provider.");
            }

            PressEnterToContinue();
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        private async Task TestConnection()
        {
            var result = await SpinnerComponents.WithSpinnerAsync(
                $"Testing connection to {_appContext.CurrentProvider} database...",
                async () => await _migrationHelper.TestConnectionAsync(),
                "Testing");

            if (result)
            {
                SafeMarkup.Success("Connection test successful!");
            }
            else
            {
                SafeMarkup.Error("Connection test failed. Please check your connection settings.");
            }

            PressEnterToContinue();
        }

        /// <summary>
        /// Configure connection string
        /// </summary>
        private async Task ConfigureConnectionString()
        {
            var currentConnectionString = _connectionManager.GetConnectionString();

            // Get connection string format based on provider
            string connectionFormat = _appContext.CurrentProvider == "PostgreSQL"
                ? "Host=localhost;Database=mydb;Username=username;Password=password"
                : "Server=localhost;Database=mydb;User Id=username;Password=password;TrustServerCertificate=true";

            SafeMarkup.Info($"Current provider: {_appContext.CurrentProvider}");
            SafeMarkup.Info($"Example format: {connectionFormat}");

            if (!string.IsNullOrEmpty(currentConnectionString))
            {
                var maskedConnectionString = MaskConnectionString(currentConnectionString);
                SafeMarkup.Info($"Current connection string: {maskedConnectionString}");
            }

            // Ask if user wants to update connection string
            if (MenuComponents.Confirm("Update connection string?", !string.IsNullOrEmpty(currentConnectionString)))
            {
                var newConnectionString = AnsiConsole.Prompt(
                    MenuComponents.CreateConnectionStringPrompt(
                        "Enter new connection string:",
                        currentConnectionString,
                        _appContext.CurrentProvider));

                _connectionManager.SetConnectionString(_appContext.CurrentProvider, newConnectionString);

                // Test the connection
                var result = await SpinnerComponents.WithSpinnerAsync(
                    "Testing new connection...",
                    async () => await _migrationHelper.TestConnectionAsync(),
                    "Testing");

                if (result)
                {
                    SafeMarkup.Success("Connection string updated and tested successfully!");
                }
                else
                {
                    SafeMarkup.Warning("Connection string updated but test failed. Please verify your connection settings.");
                }
            }

            PressEnterToContinue();
        }

        /// <summary>
        /// Mask sensitive information in connection string
        /// </summary>
        private string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            // Replace password with asterisks
            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|pwd)=([^;]*)",
                "$1=********",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private void PressEnterToContinue()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Markup("Press [grey][[Enter]][/] to continue...");
            Console.ReadLine();
        }
    }
}
