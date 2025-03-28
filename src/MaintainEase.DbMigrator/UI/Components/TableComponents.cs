using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using Spectre.Console.Rendering;

namespace MaintainEase.DbMigrator.UI.Components
{
    /// <summary>
    /// Enhanced table components for data display
    /// </summary>
    public static class TableComponents
    {
        /// <summary>
        /// Create a standard table with the specified title and columns
        /// </summary>
        public static Table CreateTable(string title, params string[] columns)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderStyle(new Style(Color.Blue))
                .Title($"[bold blue]{SafeMarkup.EscapeMarkup(title)}[/]");

            foreach (var column in columns)
            {
                table.AddColumn(new TableColumn(SafeMarkup.EscapeMarkup(column)).Centered());
            }

            return table;
        }

        /// <summary>
        /// Add a status row to a table with color-coded indicators
        /// </summary>
        public static void AddStatusRow(this Table table, string item, bool success, string message = null)
        {
            string status = success ? "[green]Success[/]" : "[red]Failed[/]";
            table.AddRow(
                new Text(SafeMarkup.EscapeMarkup(item)),
                new Markup(status),
                new Text(SafeMarkup.EscapeMarkup(message ?? string.Empty))
            );
        }

        /// <summary>
        /// Add a color-coded status row with custom status text
        /// </summary>
        public static void AddStatusRow(this Table table, string item, string status, string statusColor, string message = null)
        {
            table.AddRow(
                new Text(SafeMarkup.EscapeMarkup(item)),
                new Markup($"[{statusColor}]{SafeMarkup.EscapeMarkup(status)}[/]"),
                new Text(SafeMarkup.EscapeMarkup(message ?? string.Empty))
            );
        }

        /// <summary>
        /// Create a database information table with standard fields
        /// </summary>
        public static Table CreateDatabaseInfoTable(string title = "Database Information")
        {
            var table = CreateTable(title, "Property", "Value");
            table.Expand();
            return table;
        }

        /// <summary>
        /// Create a migration status table with standard fields
        /// </summary>
        public static Table CreateMigrationTable(string title = "Migration Status")
        {
            var table = CreateTable(title, "ID", "Name", "Created", "Status", "Applied On");
            table.Expand();
            return table;
        }

        /// <summary>
        /// Create a tenant status table with standard fields
        /// </summary>
        public static Table CreateTenantTable(string title = "Tenant Status")
        {
            var table = CreateTable(title, "Tenant", "Database", "Environment", "Status", "Migrations");
            table.Expand();
            return table;
        }

        /// <summary>
        /// Create a plugin information table
        /// </summary>
        public static Table CreatePluginsTable(string title = "Available Plugins")
        {
            var table = CreateTable(title, "Name", "Version", "Type", "Status", "Capabilities");
            table.Expand();
            return table;
        }

        /// <summary>
        /// Create a comparison results table for database objects
        /// </summary>
        public static Table CreateComparisonTable(string title = "Comparison Results")
        {
            var table = CreateTable(title, "Object", "Type", "Change", "Details");
            table.Expand();
            return table;
        }

        /// <summary>
        /// Add a header row to a table
        /// </summary>
        public static void AddHeaderRow(this Table table, params string[] values)
        {
            var markupValues = values.Select(v => new Markup($"[bold]{SafeMarkup.EscapeMarkup(v)}[/]")).ToArray();
            table.AddRow(markupValues);
        }

        /// <summary>
        /// Add a separator row to a table
        /// </summary>
        public static void AddSeparatorRow(this Table table, int columnCount)
        {
            string[] separators = new string[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                separators[i] = new string('─', 10);
            }
            
            var markupValues = separators.Select(v => new Markup($"[grey]{v}[/]")).ToArray();
            table.AddRow(markupValues);
        }

        /// <summary>
        /// Display an object as a property table
        /// </summary>
        public static void DisplayObject<T>(T obj, string title, bool expand = true)
        {
            var table = CreateTable(title, "Property", "Value");
            if (expand) table.Expand();
            
            foreach (var prop in typeof(T).GetProperties())
            {
                var value = prop.GetValue(obj);
                var displayValue = value?.ToString() ?? "null";
                
                table.AddRow(
                    new Text(SafeMarkup.EscapeMarkup(prop.Name)),
                    new Text(SafeMarkup.EscapeMarkup(displayValue))
                );
            }
            
            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Display a collection of objects as a table
        /// </summary>
        public static void DisplayCollection<T>(IEnumerable<T> collection, string title, bool expand = true, params (string Header, Func<T, string> ValueSelector)[] columns)
        {
            // Handle empty collections
            if (!collection.Any())
            {
                SafeMarkup.Info($"No items to display in '{title}'");
                return;
            }
            
            // Create table headers
            string[] headers = columns.Select(c => c.Header).ToArray();
            var table = CreateTable(title, headers);
            if (expand) table.Expand();
            
            // Add rows
            foreach (var item in collection)
            {
                var rowValues = columns.Select(c => new Text(SafeMarkup.EscapeMarkup(c.ValueSelector(item) ?? "null"))).ToArray();
                table.AddRow(rowValues);
            }
            
            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Create a table for viewing data organized in rows and columns
        /// </summary>
        public static Table CreateDataTable<T>(string title, IEnumerable<T> data, params (string Header, Func<T, object> ValueSelector)[] columns)
        {
            // Create table with headers
            string[] headers = columns.Select(c => c.Header).ToArray();
            var table = CreateTable(title, headers);
            table.Expand();
            
            // Add rows
            foreach (var item in data)
            {
                var rowValues = columns.Select(c => {
                    var value = c.ValueSelector(item);
                    return value switch
                    {
                        null => new Text("null"),
                        bool b => new Markup(b ? "[green]True[/]" : "[red]False[/]"),
                        DateTime dt => new Text(dt.ToString("yyyy-MM-dd HH:mm:ss")),
                        DateTimeOffset dto => new Text(dto.ToString("yyyy-MM-dd HH:mm:ss")),
                        Enum e => new Markup($"[blue]{e}[/]"),
                        _ => new Text(SafeMarkup.EscapeMarkup(value.ToString()))
                    };
                }).ToArray<IRenderable>();
                
                table.AddRow(rowValues);
            }
            
            return table;
        }

        /// <summary>
        /// Create a heatmap table visualizing numeric data with color gradients
        /// </summary>
        public static Table CreateHeatmapTable<T>(
            string title, 
            IEnumerable<T> data, 
            (string Header, Func<T, string> LabelSelector) rowLabels,
            params (string Header, Func<T, double> ValueSelector)[] columns)
        {
            // Create table with headers
            string[] headers = new[] { rowLabels.Header }.Concat(columns.Select(c => c.Header)).ToArray();
            var table = CreateTable(title, headers);
            table.Expand();
            
            // Find min and max values for color scaling
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            
            foreach (var item in data)
            {
                foreach (var column in columns)
                {
                    double value = column.ValueSelector(item);
                    minValue = Math.Min(minValue, value);
                    maxValue = Math.Max(maxValue, value);
                }
            }
            
            // Add rows with heatmap coloring
            foreach (var item in data)
            {
                var rowValues = new List<IRenderable>
                {
                    new Text(SafeMarkup.EscapeMarkup(rowLabels.LabelSelector(item)))
                };
                
                foreach (var column in columns)
                {
                    double value = column.ValueSelector(item);
                    string formattedValue = value.ToString("0.##");
                    
                    // Calculate color based on value relative to min/max range
                    var color = GetHeatmapColor(value, minValue, maxValue);
                    rowValues.Add(new Markup($"[{color}]{formattedValue}[/]"));
                }
                
                table.AddRow(rowValues.ToArray());
            }
            
            return table;
        }

        private static string GetHeatmapColor(double value, double min, double max)
        {
            if (min == max) return "green";
            
            // Calculate where this value falls in the range from 0 to 1
            double normalized = (value - min) / (max - min);
            
            // Assign colors based on the normalized value
            return normalized switch
            {
                var n when n >= 0.8 => "red",
                var n when n >= 0.6 => "orange1",
                var n when n >= 0.4 => "yellow",
                var n when n >= 0.2 => "lime",
                _ => "green"
            };
        }
    }
}
