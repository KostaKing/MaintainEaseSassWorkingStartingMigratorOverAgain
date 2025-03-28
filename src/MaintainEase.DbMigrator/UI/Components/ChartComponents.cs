using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;

namespace MaintainEase.DbMigrator.UI.Components
{
    /// <summary>
    /// Enhanced chart components for data visualization
    /// </summary>
    public static class ChartComponents
    {
        /// <summary>
        /// Create a bar chart for visualizing categorical data
        /// </summary>
        public static BarChart CreateBarChart(string title)
        {
            var chart = new BarChart()
                .Width(60)
                .Label($"[bold blue]{SafeMarkup.EscapeMarkup(title)}[/]")
                .CenterLabel();
                
            return chart;
        }
        
        /// <summary>
        /// Add a category to a bar chart with automatic color selection
        /// </summary>
        public static void AddBarChartCategory(
            this BarChart chart, 
            string label, 
            double value, 
            Color? color = null)
        {
            // Auto-assign color if not specified
            color ??= GetAutoColor(chart.Data.Count);
            
            chart.AddItem(SafeMarkup.EscapeMarkup(label), value, color.Value);
        }
        
        /// <summary>
        /// Create a breakdown chart for visualizing proportions
        /// </summary>
        public static BreakdownChart CreateBreakdownChart(string title)
        {
            var chart = new BreakdownChart()
                .Width(60)
                .ShowPercentage()
                .FullSize();
                
            chart.UseValueFormatter(value => $"[bold]{value:F1}%[/]");
                
            return chart;
        }
        
        /// <summary>
        /// Add a category to a breakdown chart with automatic color selection
        /// </summary>
        public static void AddBreakdownChartCategory(
            this BreakdownChart chart, 
            string label, 
            double value, 
            Color? color = null)
        {
            // Auto-assign color if not specified
            color ??= GetAutoColor(chart.Data.Count);
            
            chart.AddItem(SafeMarkup.EscapeMarkup(label), value, color.Value);
        }
        
        /// <summary>
        /// Visualize data as a bar chart
        /// </summary>
        public static void ShowBarChart<T>(
            string title,
            IEnumerable<T> data,
            Func<T, string> labelSelector,
            Func<T, double> valueSelector)
        {
            var chart = CreateBarChart(title);
            
            // Auto-sort data by value (descending)
            var sortedData = data.OrderByDescending(valueSelector).ToList();
            
            for (int i = 0; i < sortedData.Count; i++)
            {
                var item = sortedData[i];
                chart.AddBarChartCategory(
                    labelSelector(item),
                    valueSelector(item),
                    GetAutoColor(i));
            }
            
            AnsiConsole.Write(chart);
        }
        
        /// <summary>
        /// Visualize data as a breakdown chart
        /// </summary>
        public static void ShowBreakdownChart<T>(
            string title,
            IEnumerable<T> data,
            Func<T, string> labelSelector,
            Func<T, double> valueSelector)
        {
            var chart = CreateBreakdownChart(title);
            var items = data.ToList();
            
            // Calculate total for percentage
            double total = items.Sum(valueSelector);
            
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                double value = valueSelector(item);
                double percentage = (value / total) * 100;
                
                chart.AddBreakdownChartCategory(
                    $"{labelSelector(item)} ({value:F1})",
                    percentage,
                    GetAutoColor(i));
            }
            
            AnsiConsole.Write(chart);
        }
        
        /// <summary>
        /// Display a colored legend for chart categories
        /// </summary>
        public static void ShowLegend(IDictionary<string, Color> categoryColors)
        {
            var legend = new Table().Border(TableBorder.None).HideHeaders();
            legend.AddColumn(new TableColumn("Symbol"));
            legend.AddColumn(new TableColumn("Category"));
            
            foreach (var category in categoryColors)
            {
                legend.AddRow(
                    new Markup($"[{category.Value.ToMarkup()}]■[/]"),
                    new Text(SafeMarkup.EscapeMarkup(category.Key)));
            }
            
            AnsiConsole.Write(legend);
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Get an automatically assigned color based on index
        /// </summary>
        private static Color GetAutoColor(int index)
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
        
        #endregion
    }
}
