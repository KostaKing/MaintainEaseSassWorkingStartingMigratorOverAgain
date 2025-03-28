using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.Configuration;

namespace MaintainEase.DbMigrator.UI.Charts
{
    /// <summary>
    /// Factory and manager for chart creation and display
    /// </summary>
    public static class ChartManager
    {
        /// <summary>
        /// Create a bar chart with the given title
        /// </summary>
        public static BarChart CreateBarChart(string title)
        {
            return new BarChart(title);
        }

        /// <summary>
        /// Create a line chart with the given title
        /// </summary>
        public static LineChart CreateLineChart(string title)
        {
            return new LineChart(title);
        }

        /// <summary>
        /// Create a pie chart with the given title
        /// </summary>
        public static PieChart CreatePieChart(string title)
        {
            return new PieChart(title);
        }

        /// <summary>
        /// Create a gauge chart with the given title
        /// </summary>
        public static GaugeChart CreateGaugeChart(string title)
        {
            return new GaugeChart(title);
        }

        /// <summary>
        /// Create a heatmap chart with the given title
        /// </summary>
        public static HeatmapChart CreateHeatmapChart(string title)
        {
            return new HeatmapChart(title);
        }

        /// <summary>
        /// Create a demo bar chart with sample data
        /// </summary>
        public static BarChart CreateDemoBarChart(string title = "Sample Bar Chart")
        {
            var chart = new BarChart(title);
            chart.AddBar("Category A", 87);
            chart.AddBar("Category B", 45);
            chart.AddBar("Category C", 63);
            chart.AddBar("Category D", 29);
            chart.AddBar("Category E", 52);
            return chart;
        }

        /// <summary>
        /// Create a demo line chart with sample data
        /// </summary>
        public static LineChart CreateDemoLineChart(string title = "Sample Line Chart")
        {
            var chart = new LineChart(title);
            chart.YAxisTitle = "Value";
            chart.XAxisTitle = "Time";

            // Create sample time series data
            var categories = new List<string>();
            for (int i = 1; i <= 12; i++)
            {
                categories.Add($"Month {i}");
            }
            chart.SetCategories(categories);

            // Add some data series
            var random = new Random(42);  // Fixed seed for consistency

            // First series - trend with small variations
            var series1 = new List<double>();
            for (int i = 0; i < 12; i++)
            {
                series1.Add(50 + i * 5 + random.Next(-5, 6));
            }
            chart.AddSeries("Series A", series1);

            // Second series - cyclical pattern
            var series2 = new List<double>();
            for (int i = 0; i < 12; i++)
            {
                series2.Add(80 + 20 * Math.Sin(i * Math.PI / 6) + random.Next(-5, 6));
            }
            chart.AddSeries("Series B", series2);

            return chart;
        }

        /// <summary>
        /// Create a demo pie chart with sample data
        /// </summary>
        public static PieChart CreateDemoPieChart(string title = "Sample Pie Chart")
        {
            var chart = new PieChart(title);
            chart.AddSlice("Segment 1", 45);
            chart.AddSlice("Segment 2", 28);
            chart.AddSlice("Segment 3", 17);
            chart.AddSlice("Segment 4", 10);
            return chart;
        }

        /// <summary>
        /// Create a demo gauge chart with sample data
        /// </summary>
        public static GaugeChart CreateDemoGaugeChart(string title = "Sample Gauge")
        {
            var chart = new GaugeChart(title);
            chart.MinValue = 0;
            chart.MaxValue = 100;
            chart.Value = 67.5;
            chart.Unit = "%";
            return chart;
        }

        /// <summary>
        /// Create a demo heatmap chart with sample data
        /// </summary>
        public static HeatmapChart CreateDemoHeatmapChart(string title = "Sample Heatmap")
        {
            var chart = new HeatmapChart(title);

            // Create sample data
            var random = new Random(42);  // Fixed seed for consistency
            var rowLabels = new List<string> { "Row A", "Row B", "Row C", "Row D", "Row E" };
            var colLabels = new List<string> { "Col 1", "Col 2", "Col 3", "Col 4" };

            // Some pattern in the data
            var data = new List<List<double>>();
            for (int i = 0; i < 5; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < 4; j++)
                {
                    // Create a pattern where values increase diagonally
                    double value = (i + j) * 10 + random.Next(-5, 6);
                    row.Add(value);
                }
                data.Add(row);
            }

            chart.SetData(data, rowLabels, colLabels);
            return chart;
        }

        /// <summary>
        /// Create application status charts based on current application context
        /// </summary>
        public static List<BaseChart> CreateApplicationStatusCharts(ApplicationContext appContext)
        {
            var charts = new List<BaseChart>();

            // Migration Status Gauge
            var migrationGauge = new GaugeChart("Migration Status");
            migrationGauge.MinValue = 0;
            migrationGauge.MaxValue = 100;

            double completionPercentage = appContext.HasPendingMigrations
                ? 100 - ((double)appContext.PendingMigrationsCount / (appContext.PendingMigrationsCount + 10) * 100)
                : 100;

            migrationGauge.Value = completionPercentage;
            migrationGauge.Unit = "%";
            migrationGauge.InvertColorScale = false;
            charts.Add(migrationGauge);

            // Database Operations Pie Chart
            var operationsPie = new PieChart("Database Operations");
            operationsPie.AddSlice("Migrations", 45);
            operationsPie.AddSlice("Backups", 32);
            operationsPie.AddSlice("Restores", 12);
            operationsPie.AddSlice("Queries", 87);
            operationsPie.AddSlice("Schema Changes", 23);
            charts.Add(operationsPie);

            // Performance Metrics
            var perfLineChart = new LineChart("Performance Metrics");
            perfLineChart.YAxisTitle = "Time (ms)";
            perfLineChart.XAxisTitle = "Day";

            var categories = new List<string>();
            for (int i = 1; i <= 10; i++)
            {
                categories.Add($"Day {i}");
            }
            perfLineChart.SetCategories(categories);

            // Create some sample metrics
            var random = new Random(42);  // Fixed seed for consistency

            var queryTimes = new List<double>();
            var migrationTimes = new List<double>();
            for (int i = 0; i < 10; i++)
            {
                queryTimes.Add(100 + random.Next(-10, 11));
                migrationTimes.Add(350 + random.Next(-30, 31));
            }

            perfLineChart.AddSeries("Query Time", queryTimes);
            perfLineChart.AddSeries("Migration Time", migrationTimes);
            charts.Add(perfLineChart);

            // Tenant Data Bar Chart
            var tenantBarChart = new BarChart("Tenant Data Size");

            // Add some sample tenants using actual tenant names if available
            var tenants = appContext.AvailableTenants.Count > 0
                ? appContext.AvailableTenants.Take(5).ToList()
                : new List<string> { "Default", "Tenant1", "Tenant2", "Tenant3" };

            foreach (var tenant in tenants)
            {
                tenantBarChart.AddBar(tenant, random.Next(200, 1000));
            }

            charts.Add(tenantBarChart);

            // Environment Comparison Heatmap
            var envHeatmap = new HeatmapChart("Environment Comparison");

            var environments = new List<string> { "Development", "Testing", "Staging", "Production" };
            var metrics = new List<string> { "Perf Score", "Tables", "Uptime %", "Backups" };

            var heatmapData = new List<List<double>>();
            for (int i = 0; i < environments.Count; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < metrics.Count; j++)
                {
                    // Higher values in production, lower in dev
                    double value = 50 + i * 15 + random.Next(-5, 6);
                    row.Add(value);
                }
                heatmapData.Add(row);
            }

            envHeatmap.SetData(heatmapData, environments, metrics);
            charts.Add(envHeatmap);

            return charts;
        }

        /// <summary>
        /// Display all demo charts in sequence
        /// </summary>
        public static void ShowAllDemoCharts()
        {
            AnsiConsole.Clear();
            SafeMarkup.Banner("Chart Demos");
            AnsiConsole.WriteLine();

            // Bar Chart
            SafeMarkup.SectionHeader("Bar Chart Demo");
            var barChart = CreateDemoBarChart();
            barChart.Render();
            WaitForUserConfirmation();

            // Horizontal Bar Chart
            SafeMarkup.SectionHeader("Horizontal Bar Chart Demo");
            var hBarChart = CreateDemoBarChart("Horizontal Bar Chart");
            hBarChart.Orientation = BarChartOrientation.Horizontal;
            hBarChart.Render();
            WaitForUserConfirmation();

            // Line Chart
            SafeMarkup.SectionHeader("Line Chart Demo");
            var lineChart = CreateDemoLineChart();
            lineChart.Render();
            WaitForUserConfirmation();

            // Pie Chart
            SafeMarkup.SectionHeader("Pie Chart Demo");
            var pieChart = CreateDemoPieChart();
            pieChart.Render();
            WaitForUserConfirmation();

            // Gauge Chart
            SafeMarkup.SectionHeader("Gauge Chart Demo");
            var gaugeChart = CreateDemoGaugeChart();
            gaugeChart.Render();
            WaitForUserConfirmation();

            // Heatmap Chart
            SafeMarkup.SectionHeader("Heatmap Chart Demo");
            var heatmapChart = CreateDemoHeatmapChart();
            heatmapChart.Render();
            WaitForUserConfirmation();

            SafeMarkup.Success("Chart demos completed!");
        }

        private static void WaitForUserConfirmation()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Markup("Press [grey][[Enter]][/] to continue to the next chart...");
            Console.ReadLine();
            AnsiConsole.Clear();
        }
    }
}
