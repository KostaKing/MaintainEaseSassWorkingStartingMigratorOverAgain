using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using Spectre.Console.Rendering;

namespace MaintainEase.DbMigrator.UI.Components
{
    /// <summary>
    /// Advanced progress tracking components
    /// </summary>
    public static class ProgressComponents
    {
        /// <summary>
        /// Process multiple operations with a progress bar
        /// </summary>
        public static async Task ProcessWithProgressAsync<T>(
            IEnumerable<T> items, 
            Func<T, Task> processor, 
            string title = "Processing items...")
        {
            var itemsList = items.ToList(); // Materialize to get count
            
            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),    // Task description
                    new ProgressBarColumn(),        // Progress bar
                    new PercentageColumn(),         // Percentage
                    new RemainingTimeColumn(),      // Remaining time
                    new SpinnerColumn(),            // Spinner
                })
                .StartAsync(async ctx =>
                {
                    // Create the main task
                    var mainTask = ctx.AddTask(SafeMarkup.EscapeMarkup(title));
                    mainTask.MaxValue = itemsList.Count;

                    // Process each item
                    int completed = 0;
                    int failed = 0;
                    
                    foreach (var item in itemsList)
                    {
                        string itemDescription = item.ToString();
                        var itemTask = ctx.AddTask(SafeMarkup.EscapeMarkup($"Processing {itemDescription}"));
                        
                        try
                        {
                            await processor(item);
                            itemTask.Value = 1; // Mark as complete
                            itemTask.Description = $"[green]{SafeMarkup.EscapeMarkup(itemDescription)} - Completed[/]";
                            completed++;
                        }
                        catch (Exception ex)
                        {
                            itemTask.Value = 1; // Mark as complete even though it failed
                            itemTask.Description = $"[red]{SafeMarkup.EscapeMarkup(itemDescription)} - Failed: {SafeMarkup.EscapeMarkup(ex.Message)}[/]";
                            failed++;
                        }
                        
                        // Update main task
                        mainTask.Increment(1);
                    }
                    
                    // Update the main task description with the summary
                    mainTask.Description = $"{SafeMarkup.EscapeMarkup(title)} - " +
                        $"Completed: [green]{completed}[/], Failed: [red]{failed}[/], Total: {itemsList.Count}";
                });
        }

        /// <summary>
        /// Process operations in parallel with a progress bar (up to maxParallel concurrent operations)
        /// </summary>
        public static async Task ProcessParallelWithProgressAsync<T>(
            IEnumerable<T> items, 
            Func<T, Task> processor, 
            int maxParallel = 4,
            string title = "Processing items in parallel...")
        {
            var itemsList = items.ToList(); // Materialize to get count
            var semaphore = new System.Threading.SemaphoreSlim(maxParallel);
            
            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),    // Task description
                    new ProgressBarColumn(),        // Progress bar
                    new PercentageColumn(),         // Percentage
                    new RemainingTimeColumn(),      // Remaining time
                    new SpinnerColumn(),            // Spinner
                })
                .StartAsync(async ctx =>
                {
                    // Create the main task
                    var mainTask = ctx.AddTask(SafeMarkup.EscapeMarkup(title));
                    mainTask.MaxValue = itemsList.Count;
                    
                    // Create task tracking dictionary
                    var taskDict = new Dictionary<T, ProgressTask>();
                    
                    // Create all tasks first
                    foreach (var item in itemsList)
                    {
                        string itemDescription = item.ToString();
                        var itemTask = ctx.AddTask(SafeMarkup.EscapeMarkup($"Waiting to process {itemDescription}"));
                        itemTask.Value = 0;
                        itemTask.MaxValue = 1;
                        taskDict[item] = itemTask;
                    }
                    
                    // Process items with limited parallelism
                    var tasks = new List<Task>();
                    foreach (var item in itemsList)
                    {
                        await semaphore.WaitAsync();
                        
                        tasks.Add(Task.Run(async () =>
                        {
                            var itemTask = taskDict[item];
                            string itemDescription = item.ToString();
                            
                            try
                            {
                                itemTask.Description = $"Processing {SafeMarkup.EscapeMarkup(itemDescription)}";
                                await processor(item);
                                itemTask.Value = 1; // Mark as complete
                                itemTask.Description = $"[green]{SafeMarkup.EscapeMarkup(itemDescription)} - Completed[/]";
                                mainTask.Increment(1);
                            }
                            catch (Exception ex)
                            {
                                itemTask.Value = 1; // Mark as complete even though it failed
                                itemTask.Description = $"[red]{SafeMarkup.EscapeMarkup(itemDescription)} - Failed: {SafeMarkup.EscapeMarkup(ex.Message)}[/]";
                                mainTask.Increment(1);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));
                    }
                    
                    // Wait for all tasks to complete
                    await Task.WhenAll(tasks);
                });
        }

        /// <summary>
        /// Display a progress bar for file operations
        /// </summary>
        public static async Task<long> FileOperationProgressAsync(
            string fileName,
            long fileSize,
            Func<ProgressTask, Task<long>> operation,
            string operationType = "Processing")
        {
            long result = 0;
            
            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),    // Task description
                    new ProgressBarColumn(),        // Progress bar
                    new PercentageColumn(),         // Percentage
                    new FileSizeColumn(),           // Processed size
                    new DownloadedColumn(),         // Downloaded size
                    new RemainingTimeColumn(),      // Remaining time
                    new SpinnerColumn(),            // Spinner
                })
                .StartAsync(async ctx =>
                {
                    ProgressTask task = ctx.AddTask(
                        $"[cyan]{SafeMarkup.EscapeMarkup(operationType)} {SafeMarkup.EscapeMarkup(fileName)}[/]", 
                        new ProgressTaskSettings { MaxValue = fileSize });
                    
                    result = await operation(task);
                });
                
            return result;
        }

        /// <summary>
        /// Create a custom table that shows progress of multiple operations
        /// </summary>
        public static Table CreateProgressTable(string title = "Operation Progress")
        {
            var table = SafeMarkup.CreateTable("Operation", "Status", "Progress", "Details");
            
            if (!string.IsNullOrEmpty(title))
            {
                table.Title = new TableTitle(title);
            }
            
            return table;
        }

        /// <summary>
        /// Add a progress row to a progress table
        /// </summary>
        public static void AddProgressRow(this Table table, string operation, string status, 
            double progressPercentage, string details)
        {
            string progressBar = GetProgressBar(progressPercentage);
            string statusColor = GetStatusColor(status.ToLowerInvariant());
            
            table.AddRow(
                new Markup(SafeMarkup.EscapeMarkup(operation)),
                new Markup($"[{statusColor}]{SafeMarkup.EscapeMarkup(status)}[/]"),
                new Markup(progressBar),
                new Markup(SafeMarkup.EscapeMarkup(details))
            );
        }

        /// <summary>
        /// Update a progress row in a progress table
        /// </summary>
        public static void UpdateProgressRow(this Table table, int rowIndex, string operation, 
            string status, double progressPercentage, string details)
        {
            string progressBar = GetProgressBar(progressPercentage);
            string statusColor = GetStatusColor(status.ToLowerInvariant());
            
            if (rowIndex >= 0 && rowIndex < table.Rows.Count)
            {
                table.UpdateCell(rowIndex, 0, new Markup(SafeMarkup.EscapeMarkup(operation)));
                table.UpdateCell(rowIndex, 1, new Markup($"[{statusColor}]{SafeMarkup.EscapeMarkup(status)}[/]"));
                table.UpdateCell(rowIndex, 2, new Markup(progressBar));
                table.UpdateCell(rowIndex, 3, new Markup(SafeMarkup.EscapeMarkup(details)));
            }
        }

        #region Helper Methods

        private static string GetProgressBar(double percentage)
        {
            // Ensure percentage is between 0 and 100
            percentage = Math.Max(0, Math.Min(100, percentage));
            
            // Determine color based on progress
            string color = percentage switch
            {
                100 => "green",
                >= 90 => "lime",
                >= 60 => "yellow",
                >= 30 => "orange1",
                _ => "red"
            };
            
            // Calculate the number of filled segments (out of 10)
            int filledSegments = (int)Math.Round(percentage / 10);
            string bar = $"[{color}]" + new string('█', filledSegments) + "[/]" + new string('░', 10 - filledSegments);
            
            return $"{bar} {percentage:F1}%";
        }

        private static string GetStatusColor(string status)
        {
            return status switch
            {
                "completed" => "green",
                "in progress" => "yellow",
                "pending" => "blue",
                "failed" => "red",
                "warning" => "orange1",
                "skipped" => "grey",
                _ => "white"
            };
        }

        #endregion
    }

    /// <summary>
    /// Custom progress column that displays file size
    /// </summary>
    public class FileSizeColumn : ProgressColumn
    {
        /// <inheritdoc/>
        public override IRenderable Render(ProgressTask task, ProgressContext context)
        {
            var formatter = new Spectre.Console.Rendering.StringFormatter();
            formatter.AddFormatterResult(new Spectre.Console.Rendering.FormatterResult
            {
                Text = $"{GetSizeString(task.Value)} / {GetSizeString(task.MaxValue)}",
                Style = task.IsFinished ? new Style(Color.Green) : Style.Plain
            });
            return formatter;
        }

        private static string GetSizeString(double bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Custom progress column that displays downloaded size
    /// </summary>
    public class DownloadedColumn : ProgressColumn
    {
        /// <inheritdoc/>
        public override IRenderable Render(ProgressTask task, ProgressContext context)
        {
            var formatter = new Spectre.Console.Rendering.StringFormatter();
            formatter.AddFormatterResult(new Spectre.Console.Rendering.FormatterResult
            {
                Text = $"{GetSizeString(task.Value)}",
                Style = task.IsFinished ? new Style(Color.Green) : Style.Plain
            });
            return formatter;
        }

        private static string GetSizeString(double bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
    }
}
