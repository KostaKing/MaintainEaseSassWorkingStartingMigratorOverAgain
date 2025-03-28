using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;

namespace MaintainEase.DbMigrator.UI.Components
{
    /// <summary>
    /// Advanced spinner components for visualizing async operations
    /// </summary>
    public static class SpinnerComponents
    {
        // Spinner types with custom configurations
        private static readonly Dictionary<string, Spinner> _spinnerTypes = new Dictionary<string, Spinner>
        {
            ["Default"] = Spinner.Known.Dots,
            ["Migration"] = Spinner.Known.Arrow3,
            ["Database"] = Spinner.Known.Circle,
            ["Processing"] = Spinner.Known.Line,
            ["Uploading"] = Spinner.Known.BouncingBar,
            ["Downloading"] = Spinner.Known.BouncingBall,
            ["Connecting"] = Spinner.Known.Star,
            ["Analyzing"] = Spinner.Known.Aesthetic,
            ["Waiting"] = Spinner.Known.Moon,
            ["Critical"] = Spinner.Known.Pong
        };

        /// <summary>
        /// Execute an async operation with a spinner animation and error handling
        /// </summary>
        public static async Task WithSpinnerAsync(string message, Func<Task> action, string spinnerType = "Default")
        {
            Spinner spinner = GetSpinner(spinnerType);
            Style spinnerStyle = GetSpinnerStyle(spinnerType);

            try
            {
                await AnsiConsole.Status()
                    .Spinner(spinner)
                    .SpinnerStyle(spinnerStyle)
                    .StartAsync(SafeMarkup.EscapeMarkup(message), async ctx => 
                    {
                        try
                        {
                            await action();
                        }
                        catch (Exception ex)
                        {
                            ctx.Status($"[red]Error: {SafeMarkup.EscapeMarkup(ex.Message)}[/]");
                            throw;
                        }
                    });
            }
            catch (Exception ex)
            {
                SafeMarkup.Error($"Operation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute an async function with a spinner animation and return the result
        /// </summary>
        public static async Task<T> WithSpinnerAsync<T>(string message, Func<Task<T>> function, string spinnerType = "Default")
        {
            Spinner spinner = GetSpinner(spinnerType);
            Style spinnerStyle = GetSpinnerStyle(spinnerType);
            T result = default;

            try
            {
                await AnsiConsole.Status()
                    .Spinner(spinner)
                    .SpinnerStyle(spinnerStyle)
                    .StartAsync(SafeMarkup.EscapeMarkup(message), async ctx => 
                    {
                        try
                        {
                            result = await function();
                        }
                        catch (Exception ex)
                        {
                            ctx.Status($"[red]Error: {SafeMarkup.EscapeMarkup(ex.Message)}[/]");
                            throw;
                        }
                    });

                return result;
            }
            catch (Exception ex)
            {
                SafeMarkup.Error($"Operation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute an async operation with progress updates through the status context
        /// </summary>
        public static async Task WithStatusSpinnerAsync(
            string initialMessage, 
            Func<StatusContext, Task> action, 
            string spinnerType = "Default")
        {
            Spinner spinner = GetSpinner(spinnerType);
            Style spinnerStyle = GetSpinnerStyle(spinnerType);

            try
            {
                await AnsiConsole.Status()
                    .Spinner(spinner)
                    .SpinnerStyle(spinnerStyle)
                    .StartAsync(SafeMarkup.EscapeMarkup(initialMessage), async ctx => 
                    {
                        try
                        {
                            await action(ctx);
                        }
                        catch (Exception ex)
                        {
                            ctx.Status($"[red]Error: {SafeMarkup.EscapeMarkup(ex.Message)}[/]");
                            throw;
                        }
                    });
            }
            catch (Exception ex)
            {
                SafeMarkup.Error($"Operation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create a specialized spinner for database operations
        /// </summary>
        public static async Task DatabaseOperationAsync(
            string operation, 
            Func<StatusContext, Task> action)
        {
            await WithStatusSpinnerAsync(
                $"Database operation: {operation}", 
                async ctx => 
                {
                    ctx.Status($"Preparing database operation: {SafeMarkup.EscapeMarkup(operation)}");
                    await Task.Delay(500); // Visual delay for user experience
                    
                    await action(ctx);
                    
                    ctx.Status($"[green]Completed: {SafeMarkup.EscapeMarkup(operation)}[/]");
                    await Task.Delay(500); // Visual confirmation
                },
                "Database");
        }

        /// <summary>
        /// Create a specialized spinner for migration operations
        /// </summary>
        public static async Task MigrationOperationAsync(
            string operation, 
            Func<StatusContext, Task> action)
        {
            await WithStatusSpinnerAsync(
                $"Migration: {operation}", 
                async ctx => 
                {
                    ctx.Status($"Preparing migration: {SafeMarkup.EscapeMarkup(operation)}");
                    await Task.Delay(500); // Visual delay for user experience
                    
                    await action(ctx);
                    
                    ctx.Status($"[green]Migration completed: {SafeMarkup.EscapeMarkup(operation)}[/]");
                    await Task.Delay(500); // Visual confirmation
                },
                "Migration");
        }

        /// <summary>
        /// Create a specialized spinner for Azure operations
        /// </summary>
        public static async Task AzureOperationAsync(
            string operation, 
            Func<StatusContext, Task> action)
        {
            await WithStatusSpinnerAsync(
                $"Azure: {operation}", 
                async ctx => 
                {
                    ctx.Spinner(Spinner.Known.Star);
                    ctx.SpinnerStyle(new Style(Color.Blue));
                    ctx.Status($"Connecting to Azure: {SafeMarkup.EscapeMarkup(operation)}");
                    await Task.Delay(800); // Visual delay for Azure connection
                    
                    await action(ctx);
                    
                    ctx.Status($"[blue]Azure operation completed: {SafeMarkup.EscapeMarkup(operation)}[/]");
                    await Task.Delay(500); // Visual confirmation
                },
                "Connecting");
        }

        /// <summary>
        /// Create a multi-stage spinner that shows progress through multiple steps
        /// </summary>
        public static async Task MultiStageOperationAsync<T>(
            string operationTitle,
            IEnumerable<(string Description, Func<T, Task<T>> Operation)> stages,
            T initialState)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(new Style(Color.Blue))
                .StartAsync(SafeMarkup.EscapeMarkup(operationTitle), async ctx =>
                {
                    T currentState = initialState;
                    int stageNumber = 1;
                    int totalStages = stages is ICollection<(string, Func<T, Task<T>>)> collection 
                        ? collection.Count 
                        : -1; // Unknown count

                    foreach (var (description, operation) in stages)
                    {
                        string stageDisplay = totalStages > 0
                            ? $"Stage {stageNumber}/{totalStages}: {SafeMarkup.EscapeMarkup(description)}"
                            : $"Stage {stageNumber}: {SafeMarkup.EscapeMarkup(description)}";

                        ctx.Status(stageDisplay);
                        
                        try
                        {
                            currentState = await operation(currentState);
                            ctx.Status($"[green]{stageDisplay} - Completed[/]");
                            await Task.Delay(300); // Visual confirmation of completion
                        }
                        catch (Exception ex)
                        {
                            ctx.Status($"[red]{stageDisplay} - Failed: {SafeMarkup.EscapeMarkup(ex.Message)}[/]");
                            await Task.Delay(1000); // Longer pause to see the error
                            throw;
                        }

                        stageNumber++;
                    }

                    ctx.Status($"[blue]{operationTitle} completed successfully[/]");
                });
        }

        #region Helper Methods

        private static Spinner GetSpinner(string spinnerType)
        {
            if (_spinnerTypes.TryGetValue(spinnerType, out var spinner))
            {
                return spinner;
            }
            return Spinner.Known.Dots; // Default
        }

        private static Style GetSpinnerStyle(string spinnerType)
        {
            return spinnerType.ToLower() switch
            {
                "migration" => new Style(Color.Green),
                "database" => new Style(Color.Blue),
                "processing" => new Style(Color.Yellow),
                "uploading" => new Style(Color.Orange1),
                "downloading" => new Style(Color.Cyan1),
                "connecting" => new Style(Color.Purple),
                "analyzing" => new Style(Color.Teal),
                "waiting" => new Style(Color.Grey),
                "critical" => new Style(Color.Red),
                _ => new Style(Color.Blue), // Default
            };
        }

        #endregion
    }
}
