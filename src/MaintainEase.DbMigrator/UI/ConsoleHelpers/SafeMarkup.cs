using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;
using Microsoft.Extensions.Logging;

namespace MaintainEase.DbMigrator.UI.ConsoleHelpers
{
    /// <summary>
    /// Enhanced helper class for safely handling Spectre.Console markup with improved styling and error resilience
    /// </summary>
    public static class SafeMarkup
    {
        private static ILogger _logger;

        // Color palette for consistent UI look
        public static class Colors
        {
            public const string Primary = "blue";
            public const string Secondary = "cyan";
            public const string Success = "green";
            public const string Warning = "yellow";
            public const string Danger = "red";
            public const string Info = "cyan";
            public const string Muted = "grey";
            public const string Highlight = "magenta";
        }

        // UI themes for consistent styling
        public static class Themes
        {
            public static readonly Style PrimaryStyle = new Style(Color.Blue, Color.Default, Decoration.Bold);
            public static readonly Style SuccessStyle = new Style(Color.Green, Color.Default, Decoration.Bold);
            public static readonly Style WarningStyle = new Style(Color.Yellow, Color.Default, Decoration.Bold);
            public static readonly Style DangerStyle = new Style(Color.Red, Color.Default, Decoration.Bold);
            public static readonly Style InfoStyle = new Style(Color.Cyan1, Color.Default, Decoration.Bold);
            public static readonly Style MutedStyle = new Style(Color.Grey, Color.Default, Decoration.None);
            public static readonly Style HighlightStyle = new Style(Color.Magenta1, Color.Default, Decoration.Bold);
        }

        /// <summary>
        /// Initialize with a logger
        /// </summary>
        public static void Initialize(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Escape markup characters in a string to prevent parsing errors
        /// </summary>
        public static string EscapeMarkup(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Replace [ and ] with their escaped versions
            return text.Replace("[", "[[").Replace("]", "]]");
        }

        /// <summary>
        /// Escape markup characters in a string to prevent parsing errors (alternative name)
        /// </summary>
        public static string Escape(string text)
        {
            return EscapeMarkup(text);
        }

        /// <summary>
        /// Safely write a markup line, catching any markup parsing errors
        /// </summary>
        public static void MarkupLine(string markup)
        {
            try
            {
                AnsiConsole.MarkupLine(markup);
            }
            catch (Exception ex)
            {
                // Log the error if a logger is available
                _logger?.LogWarning(ex, "Error rendering markup: {Markup}", markup);

                // Fall back to plain text
                string plainText = CleanMarkup(markup);
                AnsiConsole.WriteLine(plainText);
            }
        }

        /// <summary>
        /// Safely write markup line, catching any markup parsing errors (alternative name)
        /// </summary>
        public static void WriteMarkupLine(string markup)
        {
            MarkupLine(markup);
        }

        /// <summary>
        /// Safely write markup, catching any markup parsing errors
        /// </summary>
        public static void Markup(string markup)
        {
            try
            {
                AnsiConsole.Markup(markup);
            }
            catch (Exception ex)
            {
                // Log the error if a logger is available
                _logger?.LogWarning(ex, "Error rendering markup: {Markup}", markup);

                // Fall back to plain text
                string plainText = CleanMarkup(markup);
                AnsiConsole.Write(plainText);
            }
        }

        /// <summary>
        /// Safely display success message with enhanced styling
        /// </summary>
        public static void Success(string message)
        {
            WriteMarkupLine($"[{Colors.Success} bold]✓ SUCCESS:[/] {EscapeMarkup(message)}");
        }

        /// <summary>
        /// Safely display error message with enhanced styling
        /// </summary>
        public static void Error(string message)
        {
            WriteMarkupLine($"[{Colors.Danger} bold]✗ ERROR:[/] {EscapeMarkup(message)}");
        }

        /// <summary>
        /// Safely display warning message with enhanced styling
        /// </summary>
        public static void Warning(string message)
        {
            WriteMarkupLine($"[{Colors.Warning} bold]⚠ WARNING:[/] {EscapeMarkup(message)}");
        }

        /// <summary>
        /// Safely display info message with enhanced styling
        /// </summary>
        public static void Info(string message)
        {
            WriteMarkupLine($"[{Colors.Info} bold]ℹ INFO:[/] {EscapeMarkup(message)}");
        }

        /// <summary>
        /// Safely display debug message
        /// </summary>
        public static void Debug(string message)
        {
            WriteMarkupLine($"[{Colors.Muted}]DEBUG: {EscapeMarkup(message)}[/]");
        }

        /// <summary>
        /// Highlight important text with enhanced styling
        /// </summary>
        public static void Highlight(string message, string title = null)
        {
            if (!string.IsNullOrEmpty(title))
            {
                WriteMarkupLine($"[{Colors.Highlight} bold]{title}:[/] {EscapeMarkup(message)}");
            }
            else
            {
                WriteMarkupLine($"[{Colors.Highlight} bold]{EscapeMarkup(message)}[/]");
            }
        }

        /// <summary>
        /// Create a panel with safe markup
        /// </summary>
        public static Panel CreatePanel(string content, string header = null)
        {
            try
            {
                var panel = new Panel(content);
                if (!string.IsNullOrEmpty(header))
                {
                    panel.Header = new PanelHeader(header);
                }
                panel.Border = BoxBorder.Rounded;
                panel.Padding = new Padding(1, 1, 1, 1);
                return panel;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error creating panel with content: {Content}", content);
                // Return a simple panel without any fancy formatting
                return new Panel(CleanMarkup(content));
            }
        }

        /// <summary>
        /// Create a stylized section header
        /// </summary>
        public static void SectionHeader(string title, char borderChar = '═', string color = null)
        {
            color ??= Colors.Primary;
            var escapedTitle = EscapeMarkup(title);
            var rule = new Rule($"[bold {color}]{escapedTitle}[/]")
            {
                Justification = Justify.Center,
                // Use a standard border type instead of creating a custom one
                Border = BoxBorder.Heavy
            };

            try
            {
                AnsiConsole.Write(rule);
                AnsiConsole.WriteLine();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error rendering section header: {Title}", title);
                AnsiConsole.WriteLine(escapedTitle);
                AnsiConsole.WriteLine(new string(borderChar, Math.Min(80, Console.WindowWidth)));
            }
        }

        /// <summary>
        /// Create a stylized table with proper error handling
        /// </summary>
        public static Table CreateTable(params string[] columns)
        {
            var table = new Table();
            try
            {
                // Apply consistent styling
                table.Border = TableBorder.Rounded;
                table.BorderStyle = new Style(Color.Blue);
                table.Expand = false;

                // Add columns
                foreach (var column in columns)
                {
                    table.AddColumn(new TableColumn(EscapeMarkup(column)));
                }

                return table;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error creating table with columns: {Columns}", string.Join(", ", columns));
                // Return a simple table without fancy styling
                var fallbackTable = new Table();
                foreach (var column in columns)
                {
                    fallbackTable.AddColumn(CleanMarkup(column));
                }
                return fallbackTable;
            }
        }

        /// <summary>
        /// Write an exception with safe formatting
        /// </summary>
        public static void WriteException(Exception exception, ExceptionFormats format = ExceptionFormats.Default)
        {
            try
            {
                AnsiConsole.WriteException(exception, format);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error writing exception display");
                AnsiConsole.WriteLine($"Exception: {exception.Message}");
                AnsiConsole.WriteLine(exception.ToString());
            }
        }

        /// <summary>
        /// Format string with markup safely
        /// </summary>
        public static string Format(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
                return string.Empty;

            try
            {
                // Escape all arguments
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] is string strArg)
                    {
                        args[i] = EscapeMarkup(strArg);
                    }
                }

                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                // Log the error if a logger is available
                _logger?.LogWarning(ex, "Error formatting markup: {Format} with {ArgCount} args", format, args.Length);

                // If formatting fails, return plain concatenation
                var sb = new StringBuilder(format);
                foreach (var arg in args)
                {
                    sb.Append(" ").Append(arg);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Safely display a prompt with pretty checkbox selection
        /// </summary>
        public static List<string> MultiSelectionPrompt(string title, List<string> choices, List<string> defaultSelected = null)
        {
            try
            {
                var prompt = new MultiSelectionPrompt<string>()
                    .Title(EscapeMarkup(title))
                    .NotRequired()
                    .PageSize(Math.Min(15, choices.Count + 3))
                    .MoreChoicesText("[grey](Move up and down to see more choices)[/]")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle selection, " +
                        "[green]<enter>[/] to accept)[/]");

                // Add choices with proper escaping
                foreach (var choice in choices)
                {
                    prompt.AddChoice(EscapeMarkup(choice));
                }

                // Pre-select items if requested
                if (defaultSelected != null)
                {
                    foreach (var selected in defaultSelected)
                    {
                        prompt.Select(EscapeMarkup(selected));
                    }
                }

                return AnsiConsole.Prompt(prompt);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error showing multi-selection prompt");

                // Fallback to simple selection
                var selected = new List<string>();
                AnsiConsole.WriteLine(title);

                for (int i = 0; i < choices.Count; i++)
                {
                    var choice = choices[i];
                    bool isSelected = defaultSelected?.Contains(choice) ?? false;

                    if (AnsiConsole.Confirm($"  Select {choice}?", isSelected))
                    {
                        selected.Add(choice);
                    }
                }

                return selected;
            }
        }

        /// <summary>
        /// Create a fancy display progress bar with consistent styling
        /// </summary>
        public static ProgressContext CreateProgressBar(int maxValue, string title = null)
        {
            try
            {
                // Create a progress instance with AnsiConsole
                var progress = AnsiConsole.Progress();

                // Configure styling
                progress
                    .AutoClear(false)
                    .HideCompleted(false);

                // Start the progress (which returns a ProgressContext)
                return progress.Start(ctx =>
                {
                    // Add a task within the context
                    var task = ctx.AddTask(EscapeMarkup(title ?? "Processing..."));
                    task.MaxValue = maxValue;

                    // Return the context which can be used by the caller
                    return ctx;
                });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error creating progress bar");
                throw; // Re-throw since we can't easily create a fallback ProgressContext
            }
        }

        /// <summary>
        /// Remove markup tags from a string to get plain text
        /// </summary>
        private static string CleanMarkup(string markup)
        {
            if (string.IsNullOrEmpty(markup))
                return string.Empty;

            // Simple replacement of common markup tags
            var result = markup
                .Replace("[bold]", "")
                .Replace("[/bold]", "")
                .Replace("[/]", "")
                .Replace("[green]", "")
                .Replace("[red]", "")
                .Replace("[yellow]", "")
                .Replace("[grey]", "")
                .Replace("[gray]", "")
                .Replace("[blue]", "")
                .Replace("[cyan]", "")
                .Replace("[white]", "")
                .Replace("[magenta]", "");

            // Handle more complex tags like [blue bold]
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\[[a-zA-Z0-9\s]+\]", "");

            return result;
        }

        /// <summary>
        /// Check if the console supports ANSI colors
        /// </summary>
        public static bool SupportsAnsi()
        {
            return AnsiConsole.Profile.Capabilities.Ansi;
        }

        /// <summary>
        /// Check if the console is interactive (can use prompts)
        /// </summary>
        public static bool IsInteractive()
        {
            return AnsiConsole.Profile.Capabilities.Interactive;
        }

        /// <summary>
        /// Generate a banner with the application name or other text
        /// </summary>
        public static void Banner(string text, string color = null)
        {
            color ??= Colors.Primary;
            if (!SupportsAnsi())
            {
                AnsiConsole.WriteLine(text);
                AnsiConsole.WriteLine(new string('=', text.Length));
                return;
            }

            try
            {
                // Create a FigletText object with centering
                var figletText = new FigletText(text).Centered();

                // Apply color using the color string
                switch (color.ToLower())
                {
                    case "blue":
                        figletText = figletText.Color(Color.Blue);
                        break;
                    case "green":
                        figletText = figletText.Color(Color.Green);
                        break;
                    case "red":
                        figletText = figletText.Color(Color.Red);
                        break;
                    case "yellow":
                        figletText = figletText.Color(Color.Yellow);
                        break;
                    case "cyan":
                        figletText = figletText.Color(Color.Cyan1);
                        break;
                    case "magenta":
                        figletText = figletText.Color(Color.Magenta1);
                        break;
                    case "grey":
                    case "gray":
                        figletText = figletText.Color(Color.Grey);
                        break;
                    default:
                        figletText = figletText.Color(Color.Blue); // Default to blue
                        break;
                }

                AnsiConsole.Write(figletText);
                AnsiConsole.WriteLine();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error rendering banner");
                SectionHeader(text, '=', color);
            }
        }

        /// <summary>
        /// Show text with a spinner while waiting for an action to complete
        /// </summary>
        public static void SpinnerInfo(string message, int durationMs = 1000)
        {
            try
            {
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Themes.InfoStyle)
                    .Start(EscapeMarkup(message), ctx =>
                    {
                        Task.Delay(durationMs).Wait();
                    });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error showing spinner");
                AnsiConsole.WriteLine(message);
                Task.Delay(durationMs).Wait();
            }
        }
    }
}
