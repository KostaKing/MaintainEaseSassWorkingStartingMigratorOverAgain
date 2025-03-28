using System;
using System.Collections.Generic;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;

namespace MaintainEase.DbMigrator.UI.Charts
{
    /// <summary>
    /// Base abstract class for all chart types
    /// </summary>
    public abstract class BaseChart
    {
        /// <summary>
        /// Chart title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Width of the chart in columns
        /// </summary>
        public int Width { get; set; } = 60;

        /// <summary>
        /// Height of the chart (for charts that support explicit height)
        /// </summary>
        public int Height { get; set; } = 10;

        /// <summary>
        /// Whether to show a legend with the chart
        /// </summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>
        /// Whether to show values on the chart
        /// </summary>
        public bool ShowValues { get; set; } = true;

        /// <summary>
        /// Color scheme for the chart
        /// </summary>
        public ChartColorScheme ColorScheme { get; set; } = ChartColorScheme.Default;

        /// <summary>
        /// Dictionary of data series with their labels
        /// </summary>
        protected Dictionary<string, List<double>> DataSeries { get; } = new Dictionary<string, List<double>>();

        /// <summary>
        /// Dictionary of series colors
        /// </summary>
        protected Dictionary<string, Color> SeriesColors { get; } = new Dictionary<string, Color>();

        /// <summary>
        /// List of category labels (x-axis labels)
        /// </summary>
        protected List<string> Categories { get; } = new List<string>();

        /// <summary>
        /// Constructor with title
        /// </summary>
        protected BaseChart(string title)
        {
            Title = title ?? "Chart";
        }

        /// <summary>
        /// Add a data series to the chart
        /// </summary>
        public void AddSeries(string name, IEnumerable<double> values, Color? color = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Series name cannot be empty");

            // Add or update the data series
            if (!DataSeries.ContainsKey(name))
            {
                DataSeries[name] = new List<double>();
                SeriesColors[name] = color ?? GetNextColor();
            }

            DataSeries[name].AddRange(values);
        }

        /// <summary>
        /// Add a single data point to a series
        /// </summary>
        public void AddDataPoint(string seriesName, double value, string category = null)
        {
            if (string.IsNullOrEmpty(seriesName))
                throw new ArgumentException("Series name cannot be empty");

            // Create series if it doesn't exist
            if (!DataSeries.ContainsKey(seriesName))
            {
                DataSeries[seriesName] = new List<double>();
                SeriesColors[seriesName] = GetNextColor();
            }

            // Add the data point
            DataSeries[seriesName].Add(value);

            // Add category if provided
            if (category != null && Categories.Count < GetMaxSeriesLength())
            {
                while (Categories.Count < GetMaxSeriesLength() - 1)
                {
                    Categories.Add("");  // Fill with empty categories if needed
                }
                Categories.Add(category);
            }
        }

        /// <summary>
        /// Set categories (x-axis labels)
        /// </summary>
        public void SetCategories(IEnumerable<string> categories)
        {
            Categories.Clear();
            Categories.AddRange(categories);
        }

        /// <summary>
        /// Render the chart to the console
        /// </summary>
        public virtual void Render()
        {
            if (DataSeries.Count == 0)
            {
                SafeMarkup.Warning($"No data to display for chart: {Title}");
                return;
            }

            // The base implementation does nothing - derived classes must implement this
            SafeMarkup.Warning("BaseChart.Render() called - this is an abstract class and should not be rendered directly");
        }

        /// <summary>
        /// Get the next color from the color scheme based on the number of series
        /// </summary>
        protected Color GetNextColor()
        {
            int index = SeriesColors.Count;

            return ColorScheme switch
            {
                ChartColorScheme.Default => GetDefaultSchemeColor(index),
                ChartColorScheme.Pastel => GetPastelSchemeColor(index),
                ChartColorScheme.Monochrome => GetMonochromeSchemeColor(index),
                ChartColorScheme.Rainbow => GetRainbowSchemeColor(index),
                _ => GetDefaultSchemeColor(index)
            };
        }

        /// <summary>
        /// Get the maximum length of any data series
        /// </summary>
        protected int GetMaxSeriesLength()
        {
            int maxLength = 0;
            foreach (var series in DataSeries.Values)
            {
                maxLength = Math.Max(maxLength, series.Count);
            }
            return maxLength;
        }

        /// <summary>
        /// Get min and max values across all series
        /// </summary>
        protected (double Min, double Max) GetValueRange()
        {
            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (var series in DataSeries.Values)
            {
                foreach (var value in series)
                {
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                }
            }

            // Handle case where all values are the same
            if (min == max)
            {
                min = min > 0 ? 0 : min - 1;
                max = max > 0 ? max + 1 : 0;
            }

            return (min, max);
        }

        /// <summary>
        /// Display a legend for the chart
        /// </summary>
        protected void RenderLegend()
        {
            if (!ShowLegend || DataSeries.Count == 0) return;

            var legend = new Table().Border(TableBorder.None).HideHeaders();
            legend.AddColumn(new TableColumn("Symbol"));
            legend.AddColumn(new TableColumn("Series"));

            foreach (var series in DataSeries)
            {
                legend.AddRow(
                    new Markup($"[{SeriesColors[series.Key].ToMarkup()}]■[/]"),
                    new Text(SafeMarkup.EscapeMarkup(series.Key)));
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(legend);
        }

        private Color GetDefaultSchemeColor(int index)
        {
            return index switch
            {
                0 => Color.Blue,
                1 => Color.Green,
                2 => Color.Red,
                3 => Color.Yellow,
                4 => Color.Magenta1,
                5 => Color.Cyan1,
                6 => Color.Orange1,
                7 => Color.Purple,
                8 => Color.Lime,
                9 => Color.Teal,
                _ => Color.FromInt32(index * 23 % 255)
            };
        }

        private Color GetPastelSchemeColor(int index)
        {
            return index switch
            {
                0 => Color.SkyBlue1,
                1 => Color.SpringGreen3,
                2 => Color.Pink1,
                3 => Color.MediumPurple1,
                4 => Color.LightSalmon1,
                5 => Color.LightSkyBlue1,
                6 => Color.PaleGreen1,
                7 => Color.LightPink1,
                8 => Color.LightSlateGrey,
                9 => Color.PaleTurquoise1,
                _ => Color.FromInt32((index * 25 + 150) % 255)
            };
        }

        private Color GetMonochromeSchemeColor(int index)
        {
            // Generate shades of blue
            int baseValue = 30; // Darkest color
            int step = 15;      // Step between shades
            int value = Math.Min(255, baseValue + index * step);

            return new Color((byte)value, (byte)value, (byte)255);
        }

        private Color GetRainbowSchemeColor(int index)
        {
            return index switch
            {
                0 => Color.Red,
                1 => Color.Orange1,
                2 => Color.Yellow,
                3 => Color.Green,
                4 => Color.Blue,
                5 => Color.IndianRed1,
                6 => Color.Purple,
                7 => Color.Red1,
                8 => Color.Orange3,
                9 => Color.Yellow3,
                _ => Color.FromInt32((index * 36) % 255)
            };
        }
    }

    /// <summary>
    /// Color schemes for charts
    /// </summary>
    public enum ChartColorScheme
    {
        Default,
        Pastel,
        Monochrome,
        Rainbow
    }
}
