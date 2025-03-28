using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;

namespace MaintainEase.DbMigrator.UI.Charts
{
    /// <summary>
    /// Bar chart implementation using Spectre.Console
    /// </summary>
    public class BarChart : BaseChart
    {
        /// <summary>
        /// Bar chart orientation
        /// </summary>
        public BarChartOrientation Orientation { get; set; } = BarChartOrientation.Vertical;

        /// <summary>
        /// Whether to stack bars when multiple series are present
        /// </summary>
        public bool IsStacked { get; set; } = false;

        /// <summary>
        /// Whether to show percentage values instead of absolute values
        /// </summary>
        public bool ShowPercentages { get; set; } = false;

        /// <summary>
        /// Create a new bar chart with the given title
        /// </summary>
        public BarChart(string title) : base(title)
        {
        }

        /// <summary>
        /// Render the bar chart to the console
        /// </summary>
        public override void Render()
        {
            if (DataSeries.Count == 0)
            {
                SafeMarkup.Warning($"No data to display for bar chart: {Title}");
                return;
            }

            if (IsStacked && DataSeries.Count > 1)
            {
                RenderStackedBarChart();
            }
            else if (Orientation == BarChartOrientation.Horizontal)
            {
                RenderHorizontalBarChart();
            }
            else
            {
                RenderVerticalBarChart();
            }

            // Render legend if needed
            if (ShowLegend)
            {
                RenderLegend();
            }
        }

        /// <summary>
        /// Add a single bar to the chart (single series scenario)
        /// </summary>
        public void AddBar(string label, double value, Color? color = null)
        {
            string seriesName = "Default";

            // If first item, clear default series
            if (DataSeries.ContainsKey(seriesName) && DataSeries[seriesName].Count == 0 && Categories.Count == 0)
            {
                DataSeries.Clear();
                SeriesColors.Clear();
                Categories.Clear();
            }

            // Add data to default series
            if (!DataSeries.ContainsKey(seriesName))
            {
                DataSeries[seriesName] = new List<double>();
                SeriesColors[seriesName] = color ?? GetNextColor();
            }

            // Add the value and category
            DataSeries[seriesName].Add(value);
            Categories.Add(label);
        }

        /// <summary>
        /// Add a category of bars (multi-series scenario)
        /// </summary>
        public void AddCategory(string categoryName, Dictionary<string, double> seriesValues)
        {
            // Add the category
            Categories.Add(categoryName);

            // Add data for each series
            foreach (var series in seriesValues)
            {
                if (!DataSeries.ContainsKey(series.Key))
                {
                    DataSeries[series.Key] = new List<double>();
                    SeriesColors[series.Key] = GetNextColor();
                }

                // Ensure all series have the same number of items by padding
                while (DataSeries[series.Key].Count < Categories.Count - 1)
                {
                    DataSeries[series.Key].Add(0);
                }

                // Add the value for this category
                DataSeries[series.Key].Add(series.Value);
            }

            // Make sure all series have values for this category (with 0 as default)
            foreach (var series in DataSeries.Keys)
            {
                if (DataSeries[series].Count < Categories.Count)
                {
                    DataSeries[series].Add(0);
                }
            }
        }

        private void RenderVerticalBarChart()
        {
            if (DataSeries.Count == 1)
            {
                // Simple single-series bar chart
                var series = DataSeries.First();
                var spectreChart = ChartComponents.CreateBarChart(Title);
                spectreChart.Width(Width);

                for (int i = 0; i < series.Value.Count; i++)
                {
                    string label = i < Categories.Count ? Categories[i] : $"Item {i + 1}";

                    // Format the value based on whether to show percentages
                    double value = series.Value[i];

                    spectreChart.AddBarChartCategory(
                        label,
                        value,
                        SeriesColors[series.Key]);
                }

                AnsiConsole.Write(spectreChart);
            }
            else
            {
                // Multi-series case - we need to display multiple charts or a grouped bar chart
                SafeMarkup.SectionHeader(Title);

                // For each series, create a bar chart
                foreach (var series in DataSeries)
                {
                    var spectreChart = new Spectre.Console.BarChart()
                        .Width(Width)
                        .Label($"[bold]{SafeMarkup.EscapeMarkup(series.Key)}[/]")
                        .CenterLabel();

                    for (int i = 0; i < series.Value.Count; i++)
                    {
                        string label = i < Categories.Count ? Categories[i] : $"Item {i + 1}";
                        spectreChart.AddItem(SafeMarkup.EscapeMarkup(label), series.Value[i], SeriesColors[series.Key]);
                    }

                    AnsiConsole.Write(spectreChart);
                    AnsiConsole.WriteLine();
                }
            }
        }

        private void RenderHorizontalBarChart()
        {
            // Horizontal bar charts don't have direct Spectre.Console support
            // so we'll implement our own
            SafeMarkup.SectionHeader(Title);

            // Find the maximum value for scaling
            double maxValue = 0;
            foreach (var series in DataSeries.Values)
            {
                foreach (var value in series)
                {
                    maxValue = Math.Max(maxValue, value);
                }
            }

            // Calculate the bar width based on available width
            int maxBarWidth = Width - 20; // Reserve space for labels
            if (maxBarWidth < 10) maxBarWidth = 10; // Minimum bar width

            // Handle single series
            if (DataSeries.Count == 1)
            {
                var series = DataSeries.First();
                var color = SeriesColors[series.Key];

                // Print each bar
                for (int i = 0; i < series.Value.Count; i++)
                {
                    string label = i < Categories.Count ? Categories[i] : $"Item {i + 1}";
                    double value = series.Value[i];

                    // Calculate bar length
                    int barLength = maxValue > 0
                        ? (int)Math.Round(value / maxValue * maxBarWidth)
                        : 0;

                    string bar = new string('█', barLength);
                    string valueText = ShowPercentages
                        ? $"{value:F1}%"
                        : $"{value:F1}";

                    // Print the bar
                    AnsiConsole.MarkupLine($"{SafeMarkup.EscapeMarkup(label),-15} [{color.ToMarkup()}]{bar}[/] {valueText}");
                }
            }
            else
            {
                // Multi-series case - group bars by category
                for (int i = 0; i < Categories.Count; i++)
                {
                    string category = Categories[i];
                    AnsiConsole.MarkupLine($"[bold]{SafeMarkup.EscapeMarkup(category)}[/]");

                    // Print bars for each series in this category
                    foreach (var series in DataSeries)
                    {
                        if (i < series.Value.Count)
                        {
                            double value = series.Value[i];

                            // Calculate bar length
                            int barLength = maxValue > 0
                                ? (int)Math.Round(value / maxValue * maxBarWidth)
                                : 0;

                            string bar = new string('█', barLength);
                            string valueText = ShowPercentages
                                ? $"{value:F1}%"
                                : $"{value:F1}";

                            // Print the bar with series label
                            AnsiConsole.MarkupLine($"  {SafeMarkup.EscapeMarkup(series.Key),-12} [{SeriesColors[series.Key].ToMarkup()}]{bar}[/] {valueText}");
                        }
                    }
                    AnsiConsole.WriteLine();
                }
            }
        }

        private void RenderStackedBarChart()
        {
            // Stacked bar charts need custom implementation
            SafeMarkup.SectionHeader(Title);

            // Calculate totals for each category to determine percentages
            var categoryTotals = new double[Categories.Count];

            foreach (var series in DataSeries)
            {
                for (int i = 0; i < Math.Min(series.Value.Count, Categories.Count); i++)
                {
                    categoryTotals[i] += series.Value[i];
                }
            }

            // Create a breakdown chart for each category
            for (int i = 0; i < Categories.Count; i++)
            {
                // Skip categories with no data
                if (categoryTotals[i] == 0) continue;

                var chart = ChartComponents.CreateBreakdownChart(Categories[i]);

                // Add each series value for this category
                foreach (var series in DataSeries)
                {
                    if (i < series.Value.Count && series.Value[i] > 0)
                    {
                        double value = series.Value[i];
                        double percentage = value / categoryTotals[i] * 100;

                        chart.AddBreakdownChartCategory(
                            $"{series.Key} ({value:F1})",
                            percentage,
                            SeriesColors[series.Key]);
                    }
                }

                AnsiConsole.Write(chart);
                AnsiConsole.WriteLine();
            }
        }
    }

    /// <summary>
    /// Orientation for bar charts
    /// </summary>
    public enum BarChartOrientation
    {
        Vertical,
        Horizontal
    }
}
