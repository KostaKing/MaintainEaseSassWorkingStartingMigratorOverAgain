using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;

namespace MaintainEase.DbMigrator.UI.Charts
{
    /// <summary>
    /// Line chart implementation for time series and trend data
    /// </summary>
    public class LineChart : BaseChart
    {
        /// <summary>
        /// Characters to use for plotting lines
        /// </summary>
        private static readonly char[] LineChars = { '·', '⋅', '•', '⁘', '⁙', '█' };

        /// <summary>
        /// Characters to use for plotting lines for different series
        /// </summary>
        private static readonly char[] SeriesMarkers = { '●', '■', '▲', '◆', '★', '✖', '◯', '□', '△', '◇' };

        /// <summary>
        /// Show grid lines on the chart
        /// </summary>
        public bool ShowGrid { get; set; } = true;

        /// <summary>
        /// Show markers at each data point
        /// </summary>
        public bool ShowMarkers { get; set; } = true;

        /// <summary>
        /// Show axis labels
        /// </summary>
        public bool ShowAxes { get; set; } = true;

        /// <summary>
        /// Y-axis title
        /// </summary>
        public string YAxisTitle { get; set; }

        /// <summary>
        /// X-axis title
        /// </summary>
        public string XAxisTitle { get; set; }

        /// <summary>
        /// Create a new line chart with the given title
        /// </summary>
        public LineChart(string title) : base(title)
        {
        }

        /// <summary>
        /// Render the line chart to the console
        /// </summary>
        public override void Render()
        {
            if (DataSeries.Count == 0)
            {
                SafeMarkup.Warning($"No data to display for line chart: {Title}");
                return;
            }

            SafeMarkup.SectionHeader(Title);

            // Get the value range
            var (minValue, maxValue) = GetValueRange();

            // Calculate chart dimensions
            int chartHeight = Height;
            int chartWidth = Width;

            // Calculate grid and value scaling
            double valueRange = maxValue - minValue;
            double valueStep = valueRange / chartHeight;

            // Create canvas for drawing
            var canvas = new char[chartHeight, chartWidth];

            // Initialize with spaces
            for (int y = 0; y < chartHeight; y++)
            {
                for (int x = 0; x < chartWidth; x++)
                {
                    canvas[y, x] = ' ';
                }
            }

            // Draw grid if enabled
            if (ShowGrid)
            {
                DrawGrid(canvas, chartWidth, chartHeight);
            }

            // Draw each data series
            int seriesIndex = 0;
            Dictionary<string, int> seriesIndices = new Dictionary<string, int>();

            foreach (var series in DataSeries)
            {
                seriesIndices[series.Key] = seriesIndex;
                DrawSeries(canvas, series.Key, series.Value, minValue, maxValue, chartWidth, chartHeight, seriesIndex);
                seriesIndex++;
            }

            // Render the canvas
            RenderCanvas(canvas, chartWidth, chartHeight, minValue, maxValue, valueStep);

            // Draw X-axis labels if categories exist
            if (ShowAxes && Categories.Count > 0)
            {
                DrawXAxisLabels(chartWidth);
            }

            // Render legend
            if (ShowLegend)
            {
                AnsiConsole.WriteLine();
                var legend = new Table().Border(TableBorder.None).HideHeaders();
                legend.AddColumn(new TableColumn("Symbol"));
                legend.AddColumn(new TableColumn("Series"));

                foreach (var series in DataSeries)
                {
                    char marker = seriesIndices[series.Key] < SeriesMarkers.Length
                        ? SeriesMarkers[seriesIndices[series.Key]]
                        : SeriesMarkers[0];

                    legend.AddRow(
                        new Markup($"[{SeriesColors[series.Key].ToMarkup()}]{marker}[/]"),
                        new Text(SafeMarkup.EscapeMarkup(series.Key)));
                }

                AnsiConsole.Write(legend);
            }
        }

        private void DrawGrid(char[,] canvas, int width, int height)
        {
            // Draw horizontal grid lines
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y % 2 == 0) // Draw every other row
                    {
                        canvas[y, x] = '·';
                    }
                }
            }

            // Draw vertical grid lines
            for (int x = 0; x < width; x += 5) // Every 5 columns
            {
                for (int y = 0; y < height; y++)
                {
                    canvas[y, x] = '·';
                }
            }
        }

        private void DrawSeries(char[,] canvas, string seriesName, List<double> values, double minValue,
                               double maxValue, int width, int height, int seriesIndex)
        {
            // Skip if no data
            if (values.Count == 0) return;

            // Calculate x-scaling
            double xStep = (double)width / (values.Count - 1);
            if (double.IsNaN(xStep) || double.IsInfinity(xStep)) xStep = 0;

            // Draw each point
            int lastX = -1;
            int lastY = -1;

            for (int i = 0; i < values.Count; i++)
            {
                double value = values[i];

                // Calculate position
                int x = (int)Math.Round(i * xStep);
                if (x >= width) x = width - 1;

                // Y is inverted (0 is top, height is bottom)
                double normalizedValue = (value - minValue) / (maxValue - minValue);
                int y = height - 1 - (int)Math.Round(normalizedValue * (height - 1));
                if (y < 0) y = 0;
                if (y >= height) y = height - 1;

                // Draw marker at point
                if (ShowMarkers)
                {
                    char marker = seriesIndex < SeriesMarkers.Length
                        ? SeriesMarkers[seriesIndex]
                        : SeriesMarkers[0];
                    canvas[y, x] = marker;
                }

                // Draw line to previous point
                if (lastX >= 0 && lastY >= 0)
                {
                    DrawLine(canvas, lastX, lastY, x, y, seriesIndex);
                }

                lastX = x;
                lastY = y;
            }
        }

        private void DrawLine(char[,] canvas, int x1, int y1, int x2, int y2, int seriesIndex)
        {
            // Bresenham's line algorithm
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            char lineChar = seriesIndex < SeriesMarkers.Length ? SeriesMarkers[seriesIndex] : LineChars[0];

            while (true)
            {
                if (y1 >= 0 && y1 < canvas.GetLength(0) && x1 >= 0 && x1 < canvas.GetLength(1))
                {
                    canvas[y1, x1] = lineChar;
                }

                if (x1 == x2 && y1 == y2) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }

        private void RenderCanvas(char[,] canvas, int width, int height, double minValue, double maxValue, double valueStep)
        {
            // Y-axis labels
            bool showYLabels = ShowAxes && !string.IsNullOrEmpty(YAxisTitle);
            int labelWidth = showYLabels ? 10 : 0;

            // Draw top margin with y-axis title if needed
            if (showYLabels)
            {
                AnsiConsole.Write(new String(' ', labelWidth));
                AnsiConsole.MarkupLine($"[bold]{SafeMarkup.EscapeMarkup(YAxisTitle)}[/]");
            }

            // Render each row
            for (int y = 0; y < height; y++)
            {
                if (showYLabels)
                {
                    double value = maxValue - (y * valueStep);
                    AnsiConsole.Write($"{value,8:F1} | ");
                }

                for (int x = 0; x < width; x++)
                {
                    char c = canvas[y, x];

                    // Find which series this marker belongs to
                    string seriesName = null;
                    for (int i = 0; i < SeriesMarkers.Length; i++)
                    {
                        if (c == SeriesMarkers[i])
                        {
                            // Find the series with this index
                            foreach (var series in DataSeries)
                            {
                                int idx = 0;
                                foreach (var s in DataSeries.Keys)
                                {
                                    if (s == series.Key)
                                    {
                                        if (idx == i)
                                        {
                                            seriesName = series.Key;
                                            break;
                                        }
                                        idx++;
                                    }
                                }
                                if (seriesName != null) break;
                            }
                            break;
                        }
                    }

                    if (seriesName != null && SeriesColors.ContainsKey(seriesName))
                    {
                        AnsiConsole.Markup($"[{SeriesColors[seriesName].ToMarkup()}]{c}[/]");
                    }
                    else if (c == '·')
                    {
                        AnsiConsole.Markup($"[grey]{c}[/]");
                    }
                    else
                    {
                        AnsiConsole.Write(c);
                    }
                }

                AnsiConsole.WriteLine();
            }
        }

        private void DrawXAxisLabels(int width)
        {
            // Skip if no categories
            if (Categories.Count == 0) return;

            // Calculate label spacing
            int labelCount = Math.Min(Categories.Count, 10); // Max 10 labels
            int step = Math.Max(1, Categories.Count / labelCount);

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new String(' ', 10)); // Space for y-axis labels if any

            for (int i = 0; i < Categories.Count; i += step)
            {
                if (i < Categories.Count)
                {
                    string label = Categories[i];
                    if (label.Length > 10) label = label.Substring(0, 7) + "...";

                    int position = (int)((double)i / Categories.Count * width);
                    int spacesNeeded = (i == 0) ? position : position - (10 * (i / step - 1)) - 10;
                    if (spacesNeeded > 0)
                        AnsiConsole.Write(new String(' ', spacesNeeded));

                    AnsiConsole.Markup($"[grey]{SafeMarkup.EscapeMarkup(label)}[/]");
                }
            }

            // Show x-axis title if specified
            if (!string.IsNullOrEmpty(XAxisTitle))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();
                AnsiConsole.Markup($"[bold]{SafeMarkup.EscapeMarkup(XAxisTitle)}[/]");
            }

            AnsiConsole.WriteLine();
        }
    }
}
