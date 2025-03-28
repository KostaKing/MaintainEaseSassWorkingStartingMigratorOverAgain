using System;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using System.Diagnostics.Metrics;

namespace MaintainEase.DbMigrator.UI.Charts
{
    /// <summary>
    /// Gauge chart for visualizing a single value against a scale
    /// </summary>
    public class GaugeChart : BaseChart
    {
        /// <summary>
        /// Minimum value on the scale
        /// </summary>
        public double MinValue { get; set; } = 0;

        /// <summary>
        /// Maximum value on the scale
        /// </summary>
        public double MaxValue { get; set; } = 100;

        /// <summary>
        /// Current value to display on the gauge
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Unit label to display after the value (e.g., %, MB, etc.)
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Low threshold for color change
        /// </summary>
        public double LowThreshold { get; set; } = 33;

        /// <summary>
        /// High threshold for color change
        /// </summary>
        public double HighThreshold { get; set; } = 66;

        /// <summary>
        /// Color for values below the low threshold
        /// </summary>
        public Color LowColor { get; set; } = Color.Red;

        /// <summary>
        /// Color for values between low and high thresholds
        /// </summary>
        public Color MidColor { get; set; } = Color.Yellow;

        /// <summary>
        /// Color for values above the high threshold
        /// </summary>
        public Color HighColor { get; set; } = Color.Green;

        /// <summary>
        /// Whether to invert the color scale (high values red, low values green)
        /// </summary>
        public bool InvertColorScale { get; set; } = false;

        /// <summary>
        /// Create a new gauge chart with the given title
        /// </summary>
        public GaugeChart(string title) : base(title)
        {
        }

        /// <summary>
        /// Render the gauge chart to the console
        /// </summary>
        public override void Render()
        {
            // Ensure value is within bounds
            double normalizedValue = Math.Max(MinValue, Math.Min(MaxValue, Value));

            // Calculate percentage
            double percent = (normalizedValue - MinValue) / (MaxValue - MinValue) * 100;

            // Determine display color based on value
            Color gaugeColor = GetColorForValue(normalizedValue);

            // Output the title
            AnsiConsole.MarkupLine($"[bold]{SafeMarkup.EscapeMarkup(Title)}[/]");

            // Create a simple progress bar to simulate a gauge
            int width = Width - 10; // Account for borders and padding
            int filledChars = (int)Math.Round(percent / 100 * width);
            string filled = new string('█', filledChars);
            string empty = new string('░', width - filledChars);

            // Draw the gauge
            AnsiConsole.Write("[");
            AnsiConsole.Markup($"[{gaugeColor.ToMarkup()}]{filled}[/]");
            AnsiConsole.Write(empty);
            AnsiConsole.WriteLine($"] {percent:F1}%");
            AnsiConsole.WriteLine();

            // Display the actual value
            string unitDisplay = string.IsNullOrEmpty(Unit) ? "" : $" {Unit}";
            AnsiConsole.MarkupLine($"Value: [bold]{normalizedValue:F1}{unitDisplay}[/] (Range: {MinValue:F1} - {MaxValue:F1}{unitDisplay})");

            // Add thresholds explanation if appropriate
            if (InvertColorScale)
            {
                AnsiConsole.MarkupLine($"[red]>{HighThreshold:F0}%[/] [yellow]{LowThreshold:F0}%-{HighThreshold:F0}%[/] [green]<{LowThreshold:F0}%[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]<{LowThreshold:F0}%[/] [yellow]{LowThreshold:F0}%-{HighThreshold:F0}%[/] [green]>{HighThreshold:F0}%[/]");
            }
        }

        /// <summary>
        /// Set the value and render the gauge
        /// </summary>
        public void SetValue(double value)
        {
            Value = value;
        }

        /// <summary>
        /// Get the appropriate color based on value
        /// </summary>
        private Color GetColorForValue(double value)
        {
            // Calculate percent of range
            double percent = (value - MinValue) / (MaxValue - MinValue) * 100;

            if (InvertColorScale)
            {
                if (percent >= HighThreshold) return LowColor;
                if (percent >= LowThreshold) return MidColor;
                return HighColor;
            }
            else
            {
                if (percent >= HighThreshold) return HighColor;
                if (percent >= LowThreshold) return MidColor;
                return LowColor;
            }
        }
    }
}
