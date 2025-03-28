using System;
using System.Threading.Tasks;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;

namespace MaintainEase.DbMigrator.UI.Components
{
    /// <summary>
    /// Enhanced dialog components for user interaction
    /// </summary>
    public static class DialogComponents
    {
        /// <summary>
        /// Show an information dialog with formatted content
        /// </summary>
        public static void ShowInfo(string title, string content)
        {
            var panel = new Panel(new Markup(SafeMarkup.EscapeMarkup(content)))
            {
                Header = new PanelHeader($"[bold blue]{SafeMarkup.EscapeMarkup(title)}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1, 1, 1),
                BorderStyle = new Style(Color.Blue),
                Expand = false
            };
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }
        
        /// <summary>
        /// Show a success dialog with formatted content
        /// </summary>
        public static void ShowSuccess(string title, string content)
        {
            var panel = new Panel(new Markup(SafeMarkup.EscapeMarkup(content)))
            {
                Header = new PanelHeader($"[bold green]✓ {SafeMarkup.EscapeMarkup(title)}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1, 1, 1),
                BorderStyle = new Style(Color.Green),
                Expand = false
            };
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }
        
        /// <summary>
        /// Show a warning dialog with formatted content
        /// </summary>
        public static void ShowWarning(string title, string content)
        {
            var panel = new Panel(new Markup(SafeMarkup.EscapeMarkup(content)))
            {
                Header = new PanelHeader($"[bold yellow]⚠ {SafeMarkup.EscapeMarkup(title)}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1, 1, 1),
                BorderStyle = new Style(Color.Yellow),
                Expand = false
            };
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }
        
        /// <summary>
        /// Show an error dialog with formatted content
        /// </summary>
        public static void ShowError(string title, string content)
        {
            var panel = new Panel(new Markup(SafeMarkup.EscapeMarkup(content)))
            {
                Header = new PanelHeader($"[bold red]✗ {SafeMarkup.EscapeMarkup(title)}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1, 1, 1),
                BorderStyle = new Style(Color.Red),
                Expand = false
            };
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }
        
        /// <summary>
        /// Show an exception dialog with detailed error information
        /// </summary>
        public static void ShowException(Exception exception, string title = "Error Occurred")
        {
            var panel = new Panel(new Markup($"[red]Error: {SafeMarkup.EscapeMarkup(exception.Message)}[/]\\n\\n[grey]Use ViewException() for full details[/]"))
            {
                Header = new PanelHeader($"[bold red]✗ {SafeMarkup.EscapeMarkup(title)}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1, 1, 1),
                BorderStyle = new Style(Color.Red),
                Expand = false
            };
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
            
            if (MenuComponents.Confirm("View full exception details?", false))
            {
                AnsiConsole.WriteLine();
                SafeMarkup.WriteException(exception);
                AnsiConsole.WriteLine();
            }
        }
        
        /// <summary>
        /// Show a confirmation dialog and return user choice
        /// </summary>
        public static bool ShowConfirmation(string title, string content, bool defaultValue = false)
        {
            var panel = new Panel(new Markup(SafeMarkup.EscapeMarkup(content)))
            {
                Header = new PanelHeader($"[bold cyan]? {SafeMarkup.EscapeMarkup(title)}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1, 1, 1),
                BorderStyle = new Style(Color.Cyan1),
                Expand = false
            };
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
            
            return MenuComponents.Confirm("Confirm?", defaultValue);
        }
        
        /// <summary>
        /// Show a progress dialog that runs an operation with progress updates
        /// </summary>
        public static async Task<T> ShowProgressDialog<T>(
            string title, 
            Func<ProgressContext, Task<T>> operation)
        {
            T result = default;
            
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
                    try
                    {
                        result = await operation(ctx);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.WriteLine();
                        ShowException(ex, title);
                        throw;
                    }
                });
                
            return result;
        }
        
        /// <summary>
        /// Show an input dialog that collects text input from the user
        /// </summary>
        public static string ShowInput(
            string title, 
            string prompt, 
            string defaultValue = null,
            bool isRequired = true)
        {
            var panel = new Panel(new Markup(SafeMarkup.EscapeMarkup(prompt)))
            {
                Header = new PanelHeader($"[bold blue]{SafeMarkup.EscapeMarkup(title)}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1, 1, 1),
                BorderStyle = new Style(Color.Blue),
                Expand = false
            };
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
            
            var textPrompt = MenuComponents.CreateTextPrompt(
                "Enter value:",
                defaultValue,
                isRequired);
                
            return AnsiConsole.Prompt(textPrompt);
        }

        /// <summary>
        /// Show a connection string dialog that collects a database connection string from the user
        /// </summary>
        public static string ShowConnectionStringInput(
            string title,
            string providerName,
            string defaultValue = null)
        {
            string prompt = $"Enter a connection string for {providerName}:" +
                            $"\\n\\n[grey]Example format:[/]";
                            
            if (providerName.Contains("SQL Server", StringComparison.OrdinalIgnoreCase))
            {
                prompt += $"\\n[cyan]Server=myServerAddress;Database=myDatabase;User Id=myUsername;Password=myPassword;[/]";
            }
            else if (providerName.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                prompt += $"\\n[cyan]Host=localhost;Database=myDatabase;Username=myUsername;Password=myPassword;[/]";
            }
            
            var panel = new Panel(new Markup(prompt))
            {
                Header = new PanelHeader($"[bold blue]{SafeMarkup.EscapeMarkup(title)}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1, 1, 1),
                BorderStyle = new Style(Color.Blue),
                Expand = false
            };
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
            
            var textPrompt = MenuComponents.CreateConnectionStringPrompt(
                "Connection string:",
                defaultValue,
                providerName);
                
            return AnsiConsole.Prompt(textPrompt);
        }
    }
}
