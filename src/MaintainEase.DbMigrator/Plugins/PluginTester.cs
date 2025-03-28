using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using MaintainEase.DbMigrator.Contracts.Interfaces;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Plugins
{
    /// <summary>
    /// Helper for testing plugin functionality
    /// </summary>
    public static class PluginTester
    {
        /// <summary>
        /// Run a test of the plugin system to verify functionality
        /// </summary>
        public static async Task RunPluginSystemTest(PluginService pluginService, ILogger logger)
        {
            try
            {
                // Create a panel to show test details
                var panel = new Panel(
                    new Markup("[yellow]Running Plugin System Test...[/]"))
                {
                    Header = new PanelHeader("[blue]Plugin System Test[/]"),
                    Border = BoxBorder.Rounded,
                    Padding = new Padding(1)
                };

                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();

                // Get available plugins
                var availablePlugins = pluginService.GetAvailablePlugins();

                // Create a table to display plugin information
                var table = SafeMarkup.CreateTable("Name", "Provider", "Version", "Capabilities");
                foreach (var plugin in availablePlugins)
                {
                    var capabilities = string.Join(", ", plugin.Capabilities);
                    table.AddRow(
                        SafeMarkup.EscapeMarkup(plugin.Name),
                        SafeMarkup.EscapeMarkup(plugin.ProviderType),
                        SafeMarkup.EscapeMarkup(plugin.Version),
                        SafeMarkup.EscapeMarkup(capabilities)
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Get the default plugin
                var defaultPlugin = pluginService.GetDefaultPlugin();
                SafeMarkup.Info($"Default Plugin: {defaultPlugin.Name} ({defaultPlugin.ProviderType})");
                AnsiConsole.WriteLine();

                // Test connection with each plugin
                foreach (var plugin in availablePlugins)
                {
                    var result = await SpinnerComponents.WithSpinnerAsync(
                        $"Testing connection with {plugin.Name}...",
                        async () => {
                            // Create a test request with a dummy connection string
                            var request = new MigrationRequest
                            {
                                ConnectionConfig = new ConnectionConfig
                                {
                                    ConnectionString = "Server=localhost;Database=test;User Id=test;Password=test",
                                    ProviderName = plugin.ProviderType
                                }
                            };

                            // Test the connection
                            return await plugin.MigrationHandler.TestConnectionAsync(request);
                        },
                        "Testing");

                    if (result)
                    {
                        SafeMarkup.Success($"Connection test with {plugin.Name} succeeded.");
                    }
                    else
                    {
                        SafeMarkup.Warning($"Connection test with {plugin.Name} failed. This may be expected if no real database is available.");
                    }
                }

                SafeMarkup.Success("Plugin system test completed. All plugins were loaded correctly.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error testing plugin system");
                SafeMarkup.Error($"Plugin system test failed: {ex.Message}");
            }
        }
    }
}
