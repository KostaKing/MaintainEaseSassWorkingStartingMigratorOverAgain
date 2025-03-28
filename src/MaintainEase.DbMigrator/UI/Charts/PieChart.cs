using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;

namespace MaintainEase.DbMigrator.UI.Charts
{
    /// <summary>
    /// Pie chart implementation using Spectre.Console's BreakdownChart
    /// </summary>
    public class PieChart : BaseChart
    {
        /// <summary>
        /// Individual slices of the pie
        /// </summary>
        private readonly List<PieSlice> _slices = new List<PieSlice>();

        /// <summary>
        /// Whether to show percentage values
        /// </summary>
        public bool ShowPercentages { get; set; } = true;

        /// <summary>
        /// Whether to show absolute values alongside percentages
        /// </summary>
        public bool ShowValues { get; set; } = true;

        /// <summary>
        /// Whether to sort slices by value (largest first)
        /// </summary>
        public bool SortByValue { get; set; } = true;

        /// <summary>
        /// Create a new pie chart with the given title
        /// </summary>
        public PieChart(string title) : base(title)
        {
        }

        /// <summary>
        /// Add a slice to the pie chart
        /// </summary>
        public void AddSlice(string label, double value, Color? color = null)
        {
            _slices.Add(new PieSlice
            {
                Label = label,
                Value = value,
                Color = color ?? GetNextColor()
            });
        }

        /// <summary>
        /// Render the pie chart to the console
        /// </summary>
        public override void Render()
        {
            if (_slices.Count == 0)
            {
                SafeMarkup.Warning($"No data to display for pie chart: {Title}");
                return;
            }

            // Sort slices if requested
            var slices = _slices.ToList();
            if (SortByValue)
            {
                slices = slices.OrderByDescending(s => s.Value).ToList();
            }

            // Calculate total for percentages
            double total = slices.Sum(s => s.Value);
            if (total == 0)
            {
                SafeMarkup.Warning("Cannot create pie chart with total value of 0");
                return;
            }

            // Create a breakdown chart (Spectre.Console's equivalent of pie chart)
            var chart = ChartComponents.CreateBreakdownChart(Title);

            // Add each slice
            foreach (var slice in slices)
            {
                double percentage = (slice.Value / total) * 100;
                string label = FormatSliceLabel(slice.Label, slice.Value, percentage);

                chart.AddBreakdownChartCategory(label, percentage, slice.Color);
            }

            // Display the chart
            AnsiConsole.Write(chart);

            // Show additional information table if there are many slices
            if (slices.Count > 5 && (ShowValues || ShowPercentages))
            {
                AnsiConsole.WriteLine();
                DisplaySlicesTable(slices, total);
            }
        }

        private string FormatSliceLabel(string label, double value, double percentage)
        {
            var formattedLabel = SafeMarkup.EscapeMarkup(label);

            if (ShowValues && ShowPercentages)
            {
                return $"{formattedLabel} ({value:N0}, {percentage:F1}%)";
            }
            else if (ShowValues)
            {
                return $"{formattedLabel} ({value:N0})";
            }
            else if (ShowPercentages)
            {
                return $"{formattedLabel} ({percentage:F1}%)";
            }

            return formattedLabel;
        }

        private void DisplaySlicesTable(List<PieSlice> slices, double total)
        {
            var table = SafeMarkup.CreateTable("Label", "Value", "Percentage");

            foreach (var slice in slices)
            {
                double percentage = (slice.Value / total) * 100;

                table.AddRow(
                    new Markup($"[{slice.Color.ToMarkup()}]■[/] {SafeMarkup.EscapeMarkup(slice.Label)}"),
                    new Text(slice.Value.ToString("N0")),
                    new Text($"{percentage:F1}%")
                );
            }

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// A single slice in a pie chart
        /// </summary>
        private class PieSlice
        {
            public string Label { get; set; }
            public double Value { get; set; }
            public Color Color { get; set; }
        }
    }
}
