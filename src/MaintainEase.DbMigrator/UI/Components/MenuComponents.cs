using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;

namespace MaintainEase.DbMigrator.UI.Components
{
    /// <summary>
    /// Enhanced menu components for user interaction
    /// </summary>
    public static class MenuComponents
    {
        /// <summary>
        /// Create an enhanced selection menu with custom styling
        /// </summary>
        public static SelectionPrompt<T> CreateSelectionMenu<T>(
            string title, 
            IEnumerable<T> items,
            Func<T, string> formatter)
        {
            var menu = new SelectionPrompt<T>()
                .Title(SafeMarkup.EscapeMarkup(title))
                .PageSize(10)
                .HighlightStyle(SafeMarkup.Themes.PrimaryStyle)
                .UseConverter(item => SafeMarkup.EscapeMarkup(formatter(item)));
                
            foreach (var item in items)
                menu.AddChoice(item);
                
            return menu;
        }
        
     

        /// <summary>
        /// Create a selection menu with described options
        /// </summary>
        public static SelectionPrompt<T> CreateDescriptiveMenu<T>(
        string title,
        Dictionary<T, string> itemsWithDescriptions,
        Func<T, string> formatter = null)
        {
            formatter ??= item => item.ToString();

            var menu = new SelectionPrompt<T>()
                .Title(SafeMarkup.EscapeMarkup(title))
                .PageSize(10)
                .HighlightStyle(SafeMarkup.Themes.PrimaryStyle)
                .UseConverter(item =>
                {
                    // Merge the main text with its description.
                    if (itemsWithDescriptions.TryGetValue(item, out var description))
                    {
                        return $"{SafeMarkup.EscapeMarkup(formatter(item))} - {SafeMarkup.EscapeMarkup(description)}";
                    }
                    return SafeMarkup.EscapeMarkup(formatter(item));
                });

            foreach (var item in itemsWithDescriptions.Keys)
            {
                menu.AddChoice(item);
            }

            return menu;
        }


        /// <summary>
        /// Create a multi-selection menu for selecting multiple items
        /// </summary>
        public static MultiSelectionPrompt<T> CreateMultiSelectionMenu<T>(
            string title,
            IEnumerable<T> items,
            Func<T, string> formatter,
            IEnumerable<T> defaultSelections = null)
        {
            var menu = new MultiSelectionPrompt<T>()
                .Title(SafeMarkup.EscapeMarkup(title))
                .NotRequired()
                .PageSize(15)
                .HighlightStyle(SafeMarkup.Themes.PrimaryStyle)
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle selection, " +
                    "[green]<enter>[/] to accept)[/]")
                .UseConverter(item => SafeMarkup.EscapeMarkup(formatter(item)));
                
            // Add all items
            menu.AddChoices(items);
            
            // Set default selections if provided
            if (defaultSelections != null)
            {
                foreach (var item in defaultSelections)
                {
                    menu.Select(item);
                }
            }
                
            return menu;
        }
        
        /// <summary>
        /// Create a text prompt for string input with validation
        /// </summary>
        public static TextPrompt<string> CreateTextPrompt(
            string prompt, 
            string defaultValue = null,
            bool isRequired = true,
            bool isSecret = false)
        {
            var textPrompt = new TextPrompt<string>(SafeMarkup.EscapeMarkup(prompt))
                .PromptStyle(SafeMarkup.Themes.PrimaryStyle);
                
            if (defaultValue != null)
            {
                textPrompt.DefaultValue(defaultValue);
            }
            
            if (isSecret)
            {
                textPrompt.Secret();
            }
            
            if (isRequired)
            {
                textPrompt.Validate(value => 
                    string.IsNullOrWhiteSpace(value)
                        ? ValidationResult.Error("[red]The value cannot be empty.[/]")
                        : ValidationResult.Success());
            }
                
            return textPrompt;
        }
        
        /// <summary>
        /// Create a prompt for entering a valid connection string
        /// </summary>
        public static TextPrompt<string> CreateConnectionStringPrompt(
            string prompt,
            string defaultValue = null,
            string providerName = null)
        {
            var textPrompt = new TextPrompt<string>(SafeMarkup.EscapeMarkup(prompt))
                .PromptStyle(SafeMarkup.Themes.PrimaryStyle)
                .ValidationErrorMessage("[red]Please enter a valid connection string.[/]")
                .Validate(connectionString =>
                {
                    if (string.IsNullOrWhiteSpace(connectionString))
                        return ValidationResult.Error("[red]Connection string cannot be empty.[/]");
                    
                    // Perform basic validation based on provider
                    if (!string.IsNullOrEmpty(providerName))
                    {
                        if (providerName.Contains("SQL", StringComparison.OrdinalIgnoreCase) &&
                            !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) &&
                            !connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
                        {
                            return ValidationResult.Error("[red]SQL Server connection string should include Server or Data Source.[/]");
                        }
                        else if (providerName.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) &&
                                !connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) &&
                                !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
                        {
                            return ValidationResult.Error("[red]PostgreSQL connection string should include Host or Server.[/]");
                        }
                    }
                    
                    return ValidationResult.Success();
                });
                
            if (defaultValue != null)
            {
                textPrompt.DefaultValue(defaultValue);
            }
                
            return textPrompt;
        }
        
        /// <summary>
        /// Create a prompt for selecting a file path
        /// </summary>
        public static TextPrompt<string> CreateFilePathPrompt(
            string prompt, 
            string defaultPath = null,
            bool mustExist = true,
            string fileFilter = null)
        {
            var filePrompt = new TextPrompt<string>(SafeMarkup.EscapeMarkup(prompt))
                .PromptStyle(SafeMarkup.Themes.PrimaryStyle)
                .ValidationErrorMessage("[red]Please enter a valid file path.[/]")
                .Validate(path =>
                {
                    if (string.IsNullOrWhiteSpace(path))
                        return ValidationResult.Error("[red]Path cannot be empty.[/]");
                        
                    if (mustExist && !System.IO.File.Exists(path))
                        return ValidationResult.Error("[red]File does not exist.[/]");
                    
                    if (fileFilter != null)
                    {
                        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
                        var filters = fileFilter.Split(';').Select(f => f.Trim().ToLowerInvariant());
                        
                        if (!filters.Any(f => extension.EndsWith(f)))
                            return ValidationResult.Error($"[red]File must have one of these extensions: {fileFilter}[/]");
                    }
                        
                    return ValidationResult.Success();
                });
                
            if (defaultPath != null)
            {
                filePrompt.DefaultValue(defaultPath);
            }
                
            return filePrompt;
        }
        
        /// <summary>
        /// Create a confirmation prompt with enhanced styling
        /// </summary>
        public static bool Confirm(
            string prompt,
            bool defaultValue = false)
        {
            return AnsiConsole.Prompt(
        new TextPrompt<bool>(SafeMarkup.EscapeMarkup(prompt))
        .DefaultValue(defaultValue)
        .ShowDefaultValue(true)
        .PromptStyle(SafeMarkup.Themes.PrimaryStyle)
        .AddChoice(true)
        .AddChoice(false)
        .WithConverter(choice => choice ? "y" : "n"));

        }

        /// <summary>
        /// Create a main menu with the specified options
        /// </summary>
        public static string ShowMainMenu(string title, Dictionary<string, string> options)
        {
            var menu = new SelectionPrompt<string>()
                .Title(SafeMarkup.EscapeMarkup(title))
                .PageSize(Math.Min(10, options.Count + 2))
                .HighlightStyle(SafeMarkup.Themes.PrimaryStyle);

            // Combine key and description into one display string
            foreach (var option in options)
            {
                string choiceText = $"{option.Key} - {SafeMarkup.EscapeMarkup(option.Value)}";
                menu.AddChoice(choiceText);
            }

            return AnsiConsole.Prompt(menu);
        }


        /// <summary>
        /// Create a wizard-style prompt for collecting multiple pieces of information
        /// </summary>
        public static T ShowWizard<T>(string title, T model, IEnumerable<(string Name, Func<T, string> Formatter, Func<T, TextPrompt<string>> Prompt, Action<T, string> Setter)> steps)
            where T : class
        {
            if (steps == null || !steps.Any())
            {
                SafeMarkup.Warning("No steps provided for the wizard.");
                return model;
            }
            
            SafeMarkup.SectionHeader(title);
            
            foreach (var (name, formatter, promptFactory, setter) in steps)
            {
                // Display current field value if any
                string currentValue = formatter(model);
                if (!string.IsNullOrEmpty(currentValue))
                {
                    AnsiConsole.WriteLine($"{name}: [cyan]{SafeMarkup.EscapeMarkup(currentValue)}[/]");
                    
                    // Ask if the user wants to change this value
                    if (!Confirm($"Do you want to change the {name}?", false))
                        continue;
                }
                
                // Create the prompt for this field
                var prompt = promptFactory(model);
                
                // Get the new value
                string newValue = AnsiConsole.Prompt(prompt);
                
                // Set the new value on the model
                setter(model, newValue);
            }
            
            SafeMarkup.Success($"Completed {title}");
            return model;
        }
    }
}
