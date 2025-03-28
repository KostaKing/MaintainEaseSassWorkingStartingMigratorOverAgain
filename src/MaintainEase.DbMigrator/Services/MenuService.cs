using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.UI.Menus;
using MaintainEase.DbMigrator.UI.Theme;

namespace MaintainEase.DbMigrator.Services
{
    /// <summary>
    /// Service for managing the interactive menu system
    /// </summary>
    public class MenuService
    {
        private readonly ILogger<MenuService> _logger;
        private readonly ApplicationContext _appContext;

        public MenuService(ApplicationContext appContext, ILogger<MenuService> logger)
        {
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Run the menu system
        /// </summary>
        public async Task RunAsync()
        {
            try
            {
                // Initialize the application
                await InitializeAsync();

                // Create and run the main menu
                var mainMenu = new MainMenu(_appContext, _logger);
                await mainMenu.RunAsync();

                // Cleanup when menu exits
                await CleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running menu system");
                DialogComponents.ShowException(ex, "Application Error");
            }
        }

        /// <summary>
        /// Initialize the application for interactive mode
        /// </summary>
        private async Task InitializeAsync()
        {
            // Initialize context
            _appContext.Initialize();
            _appContext.IsConsoleMode = true;
            _appContext.DetectBatchMode();

            // Initialize theme manager
            ThemeManager.Initialize();
            if (!string.IsNullOrEmpty(_appContext.PreferredTheme))
            {
                ThemeManager.SetTheme(_appContext.PreferredTheme);
            }

            // Initialize SafeMarkup
            SafeMarkup.Initialize(_logger);

            // Check for pending migrations
            await SpinnerComponents.WithSpinnerAsync(
                "Checking for pending migrations...",
                async () =>
                {
                    // This is a placeholder - in a real app, you'd check the database
                    await Task.Delay(1000);
                    _appContext.HasPendingMigrations = true;
                    _appContext.PendingMigrationsCount = 3;
                });

            // Show welcome message
            Console.Clear();
            SafeMarkup.Banner($"MaintainEase DB Migrator v{_appContext.Version}", "blue");
            await Task.Delay(1000); // Brief pause to show banner
        }

        /// <summary>
        /// Cleanup when exiting the menu system
        /// </summary>
        private async Task CleanupAsync()
        {
            await SpinnerComponents.WithSpinnerAsync(
                "Saving application state...",
                async () =>
                {
                    // Save settings
                    _appContext.SaveSettings();
                    await Task.Delay(500); // Visual delay
                });

            // Show goodbye message
            Console.Clear();
            SafeMarkup.Banner("Thank You", "green");
            SafeMarkup.Success("Thank you for using MaintainEase DB Migrator!");

            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("Exiting application...");
            await Task.Delay(1000); // Brief pause before exit
        }
    }
}
