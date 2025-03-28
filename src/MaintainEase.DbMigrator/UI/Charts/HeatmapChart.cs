using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using Spectre.Console.Rendering;

namespace MaintainEase.DbMigrator.UI.Charts
{
    /// <summary>
    /// Heatmap chart for visualizing 2D data with color intensity
    /// </summary>
    public class HeatmapChart : BaseChart
    {
        /// <summary>
        /// The 2D data grid for the heatmap
        /// </summary>
        private readonly List<List<double>> _data = new List<List<double>>();

        /// <summary>
        /// Row labels (y-axis)
        /// </summary>
        private readonly List<string> _rowLabels = new List<string>();

        /// <summary>
        /// Column labels (x-axis)
        /// </summary>
        private readonly List<string> _columnLabels = new List<string>();

        /// <summary>
        /// Minimum value in the data set (for color scaling)
        /// </summary>
        public double? MinValue { get; set; }

        /// <summary>
        /// Maximum value in the data set (for color scaling)
        /// </summary>
        public double? MaxValue { get; set; }

        /// <summary>
        /// Color for the lowest values
        /// </summary>
        public Color LowColor { get; set; } = Color.Blue;

        /// <summary>
        /// Color for the highest values
        /// </summary>
        public Color HighColor { get; set; } = Color.Red;

        /// <summary>
        /// Number format for cell values
        /// </summary>
        public string ValueFormat { get; set; } = "F1";

        /// <summary>
        /// Create a new heatmap chart with the given title
        /// </summary>
        public HeatmapChart(string title) : base(title)
        {
        }

        /// <summary>
        /// Set data for the heatmap
        /// </summary>
        public void SetData(List<List<double>> data, List<string> rowLabels = null, List<string> columnLabels = null)
        {
            _data.Clear();
            _data.AddRange(data);

            if (rowLabels != null)
            {
                _rowLabels.Clear();
                _rowLabels.AddRange(rowLabels);
            }

            if (columnLabels != null)
            {
                _columnLabels.Clear();
                _columnLabels.AddRange(columnLabels);
            }
        }

        /// <summary>
        /// Add a row of data to the heatmap
        /// </summary>
        public void AddRow(string label, List<double> rowData)
        {
            _rowLabels.Add(label);
            _data.Add(new List<double>(rowData));

            // Add default column labels if needed
            while (_columnLabels.Count < rowData.Count)
            {
                _columnLabels.Add($"Col {_columnLabels.Count + 1}");
            }
        }

        /// <summary>
        /// Set column labels
        /// </summary>
        public void SetColumnLabels(List<string> labels)
        {
            _columnLabels.Clear();
            _columnLabels.AddRange(labels);
        }

        /// <summary>
        /// Render the heatmap chart to the console
        /// </summary>
        public override void Render()
        {
            if (_data.Count == 0)
            {
                SafeMarkup.Warning($"No data to display for heatmap chart: {Title}");
                return;
            }

            // Determine min and max values for color scaling
            double min = MinValue ?? double.MaxValue;
            double max = MaxValue ?? double.MinValue;

            if (!MinValue.HasValue || !MaxValue.HasValue)
            {
                foreach (var row in _data)
                {
                    foreach (var value in row)
                    {
                        if (!MinValue.HasValue) min = Math.Min(min, value);
                        if (!MaxValue.HasValue) max = Math.Max(max, value);
                    }
                }
            }

            // Create a table for the heatmap
            var table = new Table()
                .Border(TableBorder.Square)
                .BorderStyle(new Style(Color.Grey))
                .Title($"[bold]{SafeMarkup.EscapeMarkup(Title)}[/]");

            // Add column headers
            table.AddColumn(new TableColumn(""));

            foreach (var label in _columnLabels)
            {
                table.AddColumn(new TableColumn($"[bold]{SafeMarkup.EscapeMarkup(label)}[/]").Centered());
            }

            // Add data rows
            for (int row = 0; row < _data.Count; row++)
            {
                var rowLabel = row < _rowLabels.Count ? _rowLabels[row] : $"Row {row + 1}";
                var cells = new List<IRenderable>
                {
                    new Markup($"[bold]{SafeMarkup.EscapeMarkup(rowLabel)}[/]")
                };

                for (int col = 0; col < _data[row].Count; col++)
                {
                    double value = _data[row][col];
                    var color = GetHeatmapColor(value, min, max);
                    cells.Add(new Markup($"[{color.ToMarkup()} bold]{value.ToString(ValueFormat)}[/]"));
                }

                table.AddRow(cells);
            }

            // Render the table
            AnsiConsole.Write(table);

            // Show color scale
            ShowColorScale(min, max);
        }

        private void ShowColorScale(double min, double max)
        {
            AnsiConsole.WriteLine();

            // Show color gradient
            AnsiConsole.Write("Color scale: ");

            int steps = 10;
            for (int i = 0; i <= steps; i++)
            {
                double value = min + ((max - min) * i / steps);
                var color = GetHeatmapColor(value, min, max);
                AnsiConsole.Markup($"[{color.ToMarkup()}]█[/]");
            }

            // Show range values
            AnsiConsole.MarkupLine($" {min.ToString(ValueFormat)} to {max.ToString(ValueFormat)}");
        }

        private Color GetHeatmapColor(double value, double min, double max)
        {
            // Handle edge cases
            if (min == max) return LowColor;
            if (value <= min) return LowColor;
            if (value >= max) return HighColor;

            // Calculate where this value falls in the range from 0 to 1
            double normalized = (value - min) / (max - min);

            // Linear interpolation between low and high colors
            int r = (int)Math.Round(LowColor.R + (HighColor.R - LowColor.R) * normalized);
            int g = (int)Math.Round(LowColor.G + (HighColor.G - LowColor.G) * normalized);
            int b = (int)Math.Round(LowColor.B + (HighColor.B - LowColor.B) * normalized);

            // Ensure RGB values are within valid range
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));

            return new Color((byte)r, (byte)g, (byte)b);
        }
    }
}
