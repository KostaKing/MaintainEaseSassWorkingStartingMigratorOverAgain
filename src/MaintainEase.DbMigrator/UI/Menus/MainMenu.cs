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
using MaintainEase.DbMigrator.UI.Charts;
using MaintainEase.DbMigrator.Commands.Database;
using MaintainEase.DbMigrator.Commands.Migration;
using MaintainEase.DbMigrator.Commands;
using System.Linq;
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

            var options = new Dictionary<string, string>
            {
                ["1"] = "Dashboard Overview",
                ["2"] = "Database Growth Chart",
                ["3"] = "Migration Timeline",
                ["4"] = "Performance Dashboard",
                ["5"] = "Tenant Comparison",
                ["6"] = "Environment Health Check",
                ["7"] = "All Demo Charts",
                ["0"] = "Back to Main Menu"
            };

            string choice = MenuComponents.ShowMainMenu("Visualization & Reports", options);
            string key = choice.Split(' ')[0];

            switch (key)
            {
                case "1":
                    ShowDashboardOverview();
                    break;
                case "2":
                    ShowDatabaseGrowthChart();
                    break;
                case "3":
                    ShowMigrationTimeline();
                    break;
                case "4":
                    ShowPerformanceDashboard();
                    break;
                case "5":
                    ShowTenantComparison();
                    break;
                case "6":
                    ShowEnvironmentHealthCheck();
                    break;
                case "7":
                    ChartManager.ShowAllDemoCharts();
                    break;
                case "0":
                    return;
                default:
                    DialogComponents.ShowInfo("Feature Coming Soon",
                        "This visualization feature will be implemented in a future update.");
                    break;
            }

            PressEnterToContinue();
        }

        /// <summary>
        /// Show a dashboard overview with multiple charts
        /// </summary>
        private void ShowDashboardOverview()
        {
            AnsiConsole.Clear();
            SafeMarkup.Banner("Dashboard Overview");
            AnsiConsole.WriteLine();

            // Create status charts based on application context
            var charts = ChartManager.CreateApplicationStatusCharts(_appContext);

            // Display migration status gauge
            var migrationGauge = charts.FirstOrDefault(c => c is GaugeChart) as GaugeChart;
            if (migrationGauge != null)
            {
                migrationGauge.Render();
                AnsiConsole.WriteLine();
            }

            // Display tenant data bar chart
            var tenantBarChart = charts.FirstOrDefault(c => c.Title == "Tenant Data Size") as MaintainEase.DbMigrator.UI.Charts.BarChart;
            if (tenantBarChart != null)
            {
                tenantBarChart.Render();
                AnsiConsole.WriteLine();
            }

            // Display database operations pie chart
            var operationsPie = charts.FirstOrDefault(c => c is PieChart) as PieChart;
            if (operationsPie != null)
            {
                operationsPie.Render();
                AnsiConsole.WriteLine();
            }

            SafeMarkup.Info("Dashboard shows a snapshot of current system status.");
        }

        /// <summary>
        /// Show database growth chart
        /// </summary>
        private void ShowDatabaseGrowthChart()
        {
            AnsiConsole.Clear();
            SafeMarkup.Banner("Database Growth");
            AnsiConsole.WriteLine();

            // Create a line chart for database growth
            var growthChart = new LineChart("Database Size Growth");
            growthChart.YAxisTitle = "Size (MB)";
            growthChart.XAxisTitle = "Month";

            // Add categories for months
            var months = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            growthChart.SetCategories(months);

            // Add data series for different database components
            var random = new Random(42);  // Fixed seed for consistency

            // Total size with growth trend
            var totalSize = new List<double>();
            var dataSize = new List<double>();
            var indexSize = new List<double>();
            var logSize = new List<double>();

            // Base values
            double baseTotalSize = 1000;
            double baseDataSize = 800;
            double baseIndexSize = 150;
            double baseLogSize = 50;

            // Growth rates
            double totalGrowthRate = 0.1;  // 10% per month
            double dataGrowthRate = 0.11;
            double indexGrowthRate = 0.08;
            double logGrowthRate = 0.05;

            // Generate data with some randomness
            for (int i = 0; i < 12; i++)
            {
                double dataSizeVal = baseDataSize * Math.Pow(1 + dataGrowthRate, i) + random.Next(-20, 21);
                double indexSizeVal = baseIndexSize * Math.Pow(1 + indexGrowthRate, i) + random.Next(-10, 11);
                double logSizeVal = baseLogSize * Math.Pow(1 + logGrowthRate, i) + random.Next(-5, 6);
                double totalSizeVal = baseTotalSize * Math.Pow(1 + totalGrowthRate, i) + random.Next(-15, 16);

                dataSize.Add(dataSizeVal);
                indexSize.Add(indexSizeVal);
                logSize.Add(logSizeVal);
                totalSize.Add(totalSizeVal);
            }

            growthChart.AddSeries("Total Size", totalSize);
            growthChart.AddSeries("Data Size", dataSize);
            growthChart.AddSeries("Index Size", indexSize);
            growthChart.AddSeries("Log Size", logSize);

            // Render the chart
            growthChart.Render();
            AnsiConsole.WriteLine();

            // Additional table with projected growth
            var table = TableComponents.CreateTable("Month", "Total Size (MB)", "Growth Rate (%)", "Projected Next Month (MB)");

            for (int i = 6; i < 12; i++)
            {
                double monthlyGrowthRate = (i > 0) ? ((totalSize[i] / totalSize[i - 1]) - 1) * 100 : 0;
                double projectedNext = totalSize[i] * (1 + (monthlyGrowthRate / 100));

                table.AddRow(
                    months[i],
                    $"{totalSize[i]:F0}",
                    $"{monthlyGrowthRate:F1}%",
                    $"{projectedNext:F0}"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            SafeMarkup.Info("Chart shows the growth trend of database size over the past year.");
        }

        /// <summary>
        /// Show migration timeline
        /// </summary>
        private void ShowMigrationTimeline()
        {
            AnsiConsole.Clear();
            SafeMarkup.Banner("Migration Timeline");
            AnsiConsole.WriteLine();

            // Create a table to show migration history
            var table = TableComponents.CreateMigrationTable("Migration History");

            // Add sample migration data
            table.AddRow(
                "001",
                "InitialSchema",
                DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
                "[green]Success[/]",
                DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd HH:mm")
            );
            table.AddRow(
                "002",
                "AddUserTable",
                DateTime.UtcNow.AddDays(-25).ToString("yyyy-MM-dd"),
                "[green]Success[/]",
                DateTime.UtcNow.AddDays(-25).ToString("yyyy-MM-dd HH:mm")
            );
            table.AddRow(
                "003",
                "AddProductTable",
                DateTime.UtcNow.AddDays(-15).ToString("yyyy-MM-dd"),
                "[green]Success[/]",
                DateTime.UtcNow.AddDays(-15).ToString("yyyy-MM-dd HH:mm")
            );
            table.AddRow(
                "004",
                "AddOrderTable",
                DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd"),
                "[green]Success[/]",
                DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd HH:mm")
            );
            table.AddRow(
                "005",
                "AddIndexes",
                DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
                "[yellow]Pending[/]",
                "-"
            );

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Show a bar chart of migration durations
            var durationChart = new MaintainEase.DbMigrator.UI.Charts.BarChart("Migration Duration");
            durationChart.AddBar("InitialSchema", 120);
            durationChart.AddBar("AddUserTable", 45);
            durationChart.AddBar("AddProductTable", 75);
            durationChart.AddBar("AddOrderTable", 60);
            durationChart.Render();

            AnsiConsole.WriteLine();
            SafeMarkup.Info("Timeline shows the history of migrations applied to the database.");
        }

        /// <summary>
        /// Show performance dashboard
        /// </summary>
        private void ShowPerformanceDashboard()
        {
            AnsiConsole.Clear();
            SafeMarkup.Banner("Performance Dashboard");
            AnsiConsole.WriteLine();

            // Create a gauge chart for overall performance score
            var performanceGauge = new GaugeChart("Overall Performance Score");
            performanceGauge.MinValue = 0;
            performanceGauge.MaxValue = 100;
            performanceGauge.Value = 78.5;
            performanceGauge.LowThreshold = 50;
            performanceGauge.HighThreshold = 80;
            performanceGauge.Render();

            AnsiConsole.WriteLine();

            // Create a heatmap for query performance by table
            var heatmap = new MaintainEase.DbMigrator.UI.Charts.HeatmapChart("Query Performance by Table (ms)");

            var tables = new List<string> { "Users", "Products", "Orders", "Inventory", "Transactions" };
            var operations = new List<string> { "Select", "Insert", "Update", "Delete" };

            var random = new Random(123);
            var heatmapData = new List<List<double>>();

            for (int i = 0; i < tables.Count; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < operations.Count; j++)
                {
                    // Different operations have different baseline performance
                    double baseValue = j switch
                    {
                        0 => 50,  // Select baseline
                        1 => 75,  // Insert baseline
                        2 => 85,  // Update baseline
                        3 => 70,  // Delete baseline
                        _ => 50
                    };

                    // Tables have different complexity
                    double tableMultiplier = 0.8 + (i * 0.15);

                    double value = baseValue * tableMultiplier + random.Next(-10, 11);
                    row.Add(value);
                }
                heatmapData.Add(row);
            }

            heatmap.SetData(heatmapData, tables, operations);
            heatmap.LowColor = Color.Green;  // Low values (fast) are green
            heatmap.HighColor = Color.Red;   // High values (slow) are red
            heatmap.Render();

            AnsiConsole.WriteLine();

            // Show a line chart of performance over time
            var lineChart = new LineChart("Query Response Time Trend");
            lineChart.YAxisTitle = "Response Time (ms)";
            lineChart.XAxisTitle = "Week";

            var weeks = new List<string>();
            for (int i = 1; i <= 10; i++)
            {
                weeks.Add($"Week {i}");
            }
            lineChart.SetCategories(weeks);

            // Generate trend data for different query types
            var selectTimes = new List<double>();
            var complexTimes = new List<double>();

            for (int i = 0; i < 10; i++)
            {
                // Simple queries get faster with optimization
                selectTimes.Add(50 - i * 2 + random.Next(-5, 6));

                // Complex queries might vary more
                complexTimes.Add(200 - i * 5 + random.Next(-20, 21));
            }

            lineChart.AddSeries("Simple Queries", selectTimes);
            lineChart.AddSeries("Complex Queries", complexTimes);
            lineChart.Render();

            AnsiConsole.WriteLine();
            SafeMarkup.Info("Performance dashboard shows database query and operation metrics.");
        }

        /// <summary>
        /// Show tenant comparison
        /// </summary>
        private void ShowTenantComparison()
        {
            AnsiConsole.Clear();
            SafeMarkup.Banner("Tenant Comparison");
            AnsiConsole.WriteLine();

            // Use actual tenant names if available, otherwise use defaults
            var tenants = _appContext.AvailableTenants.Count > 0
                ? _appContext.AvailableTenants.Take(5).ToList()
                : new List<string> { "Default", "Tenant1", "Tenant2", "Tenant3", "Tenant4" };

            // Create a bar chart for data sizes
            var sizeChart = new MaintainEase.DbMigrator.UI.Charts.BarChart("Data Size by Tenant (MB)");
            var random = new Random(456);

            foreach (var tenant in tenants)
            {
                sizeChart.AddBar(tenant, random.Next(500, 5000));
            }

            sizeChart.Render();
            AnsiConsole.WriteLine();

            // Create a bar chart for record counts with multiple series
            var recordsChart = new MaintainEase.DbMigrator.UI.Charts.BarChart("Record Counts by Tenant");

            // For each tenant, add a category
            for (int i = 0; i < tenants.Count; i++)
            {
                var seriesValues = new Dictionary<string, double>
                {
                    ["Users"] = random.Next(100, 1000),
                    ["Products"] = random.Next(1000, 10000),
                    ["Orders"] = random.Next(5000, 50000),
                    ["Transactions"] = random.Next(10000, 100000)
                };

                recordsChart.AddCategory(tenants[i], seriesValues);
            }

            recordsChart.Orientation = BarChartOrientation.Horizontal;
            recordsChart.Render();
            AnsiConsole.WriteLine();

            // Create a pie chart for tenant storage distribution
            var storagePie = new PieChart("Storage Distribution by Tenant");

            double totalStorage = 0;
            var storageSizes = new List<double>();

            // Generate random storage sizes
            for (int i = 0; i < tenants.Count; i++)
            {
                double storage = random.Next(500, 5000);
                storageSizes.Add(storage);
                totalStorage += storage;
            }

            // Add slices for each tenant
            for (int i = 0; i < tenants.Count; i++)
            {
                storagePie.AddSlice(tenants[i], storageSizes[i]);
            }

            storagePie.Render();
            AnsiConsole.WriteLine();

            SafeMarkup.Info("Tenant comparison shows data distribution across different tenants.");
        }

        /// <summary>
        /// Show environment health check
        /// </summary>
        private void ShowEnvironmentHealthCheck()
        {
            AnsiConsole.Clear();
            SafeMarkup.Banner("Environment Health Check");
            AnsiConsole.WriteLine();

            // Create a heatmap for environment comparison
            var heatmap = new MaintainEase.DbMigrator.UI.Charts.HeatmapChart("Environment Health Metrics");

            var environments = new List<string> { "Development", "Testing", "Staging", "Production" };
            var metrics = new List<string> { "Uptime %", "Response (ms)", "Errors", "CPU Load" };

            var random = new Random(789);
            var data = new List<List<double>>();

            for (int i = 0; i < environments.Count; i++)
            {
                var row = new List<double>();

                // Uptime % - higher is better
                row.Add(95 + (i * 1.2) + random.Next(-2, 3));

                // Response time - lower is better
                row.Add(200 - (i * 15) + random.Next(-20, 21));

                // Error count - lower is better
                row.Add(20 - (i * 5) + random.Next(-3, 4));

                // CPU load - moderate is ideal
                row.Add(40 + (i * 10) + random.Next(-10, 11));

                data.Add(row);
            }

            heatmap.SetData(data, environments, metrics);
            heatmap.LowColor = Color.Green;  // Low values (fast) are green
            heatmap.HighColor = Color.Red;   // High values (slow) are red
            heatmap.Render();

            AnsiConsole.WriteLine();

            // Display individual environment health gauges
            foreach (var env in environments)
            {
                var healthScore = env switch
                {
                    "Development" => random.Next(70, 90),
                    "Testing" => random.Next(75, 95),
                    "Staging" => random.Next(80, 98),
                    "Production" => random.Next(90, 100),
                    _ => random.Next(70, 100)
                };

                var gauge = new GaugeChart($"{env} Health Score");
                gauge.MinValue = 0;
                gauge.MaxValue = 100;
                gauge.Value = healthScore;
                gauge.Unit = "%";
                gauge.Render();
                AnsiConsole.WriteLine();
            }

            SafeMarkup.Info("Environment health check shows the status across different environments.");
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

        /// <summary>
        /// Show demo visualizations (simple version for compatibility)
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
    }
}
