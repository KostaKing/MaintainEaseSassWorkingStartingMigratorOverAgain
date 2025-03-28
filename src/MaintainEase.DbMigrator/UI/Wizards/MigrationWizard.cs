using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.Configuration;

namespace MaintainEase.DbMigrator.UI.Wizards
{
    /// <summary>
    /// Wizard for configuring and running database migrations
    /// </summary>
    public class MigrationWizard
    {
        /// <summary>
        /// Run the migration configuration wizard
        /// </summary>
        public async Task<MigrationConfig> RunAsync(MigrationConfig existingConfig = null)
        {
            // Create a new migration config or use the existing one
            var config = existingConfig ?? new MigrationConfig
            {
                Version = "1.0",
                Timestamp = DateTime.UtcNow,
                Options = new MigrationOptions()
            };
            
            SafeMarkup.Banner("Database Migration Wizard");
            SafeMarkup.Info("This wizard will help you configure and run database migrations.");
            
            // Step 1: Configure basic migration info
            ConfigureBasicInfo(config);
            
            // Step 2: Configure migration options
            ConfigureOptions(config);
            
            // Step 3: Select migrations to run
            await SelectMigrationsAsync(config);
            
            // Step 4: Confirm and summary
            if (ShowSummaryAndConfirm(config))
            {
                SafeMarkup.Success("Migration configuration completed successfully!");
                return config;
            }
            else
            {
                if (DialogComponents.ShowConfirmation(
                    "Configuration Cancelled",
                    "Would you like to restart the configuration?",
                    false))
                {
                    // Recursively run the wizard again
                    return await RunAsync(config);
                }
                else
                {
                    SafeMarkup.Warning("Migration configuration cancelled.");
                    return null;
                }
            }
        }
        
        private void ConfigureBasicInfo(MigrationConfig config)
        {
            SafeMarkup.SectionHeader("Migration Information");
            
            // Migration name
            config.Name = AnsiConsole.Prompt(
                MenuComponents.CreateTextPrompt(
                    "Migration name:",
                    config.Name ?? $"Migration_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                    true));
            
            // Migration description
            config.Description = AnsiConsole.Prompt(
                MenuComponents.CreateTextPrompt(
                    "Description (optional):",
                    config.Description,
                    false));
            
            // Environment
            var environments = new Dictionary<string, string>
            {
                ["Development"] = "Local development environment",
                ["Testing"] = "Testing/QA environment",
                ["Staging"] = "Pre-production staging environment",
                ["Production"] = "Production environment"
            };
            
            config.Environment = AnsiConsole.Prompt(
                MenuComponents.CreateDescriptiveMenu(
                    "Target environment:",
                    environments));
        }
        
        private void ConfigureOptions(MigrationConfig config)
        {
            SafeMarkup.SectionHeader("Migration Options");
            
            // Transaction mode
            var transactionModes = new Dictionary<string, string>
            {
                ["PerMigration"] = "Use a separate transaction for each migration",
                ["SingleTransaction"] = "Use a single transaction for all migrations",
                ["NoTransaction"] = "Run without transaction support"
            };
            
            config.Options.TransactionMode = AnsiConsole.Prompt(
                MenuComponents.CreateDescriptiveMenu(
                    "Transaction mode:",
                    transactionModes));
            
            // Script generation
            config.Options.GenerateScripts = MenuComponents.Confirm(
                "Generate SQL scripts for migrations?",
                config.Options.GenerateScripts);
            
            if (config.Options.GenerateScripts)
            {
                config.Options.ScriptOutputPath = AnsiConsole.Prompt(
                    MenuComponents.CreateTextPrompt(
                        "Script output path:",
                        config.Options.ScriptOutputPath ?? "./Scripts"));
            }
            
            // Timeout
            config.Options.CommandTimeout = AnsiConsole.Prompt(
                new TextPrompt<int>("Command timeout in seconds:")
                    .DefaultValue(config.Options.CommandTimeout)
                    .ValidationErrorMessage("[red]Please enter a valid timeout (5-600 seconds).[/]")
                    .Validate(timeout => 
                        timeout < 5 || timeout > 600 
                            ? ValidationResult.Error("[red]Timeout must be between 5 and 600 seconds.[/]") 
                            : ValidationResult.Success()));
            
            // Backup
            config.Options.CreateBackup = MenuComponents.Confirm(
                "Create database backup before migrations?",
                config.Options.CreateBackup);
        }
        
        private async Task SelectMigrationsAsync(MigrationConfig config)
        {
            SafeMarkup.SectionHeader("Select Migrations");
            
            // Simulate loading available migrations
            var availableMigrations = await LoadAvailableMigrationsAsync();
            
            if (availableMigrations.Count == 0)
            {
                SafeMarkup.Warning("No pending migrations found.");
                return;
            }
            
            // Display available migrations
            var table = TableComponents.CreateMigrationTable("Available Migrations");
            foreach (var migration in availableMigrations)
            {
                table.AddRow(
                    migration.Id.ToString(),
                    migration.Name,
                    migration.Created.ToString("yyyy-MM-dd HH:mm"),
                    migration.IsApplied ? "[green]Applied[/]" : "[yellow]Pending[/]",
                    migration.AppliedOn?.ToString("yyyy-MM-dd HH:mm") ?? "-"
                );
            }
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            
            // Filter only pending migrations
            var pendingMigrations = availableMigrations
                .Where(m => !m.IsApplied)
                .ToList();
            
            if (pendingMigrations.Count == 0)
            {
                SafeMarkup.Success("All migrations have been applied!");
                return;
            }
            
            // Select migrations to run
            var migrationOptions = pendingMigrations
                .Select(m => $"{m.Id}: {m.Name} ({m.Created:yyyy-MM-dd})")
                .ToList();
            
            var selectedMigrations = AnsiConsole.Prompt(
                MenuComponents.CreateMultiSelectionMenu(
                    "Select migrations to run:",
                    migrationOptions,
                    m => m,
                    migrationOptions)); // By default select all
            
            // Map selected migrations back to IDs
            config.MigrationIds = selectedMigrations
                .Select(s => int.Parse(s.Split(':')[0]))
                .ToList();
        }
        
        private bool ShowSummaryAndConfirm(MigrationConfig config)
        {
            SafeMarkup.SectionHeader("Migration Summary");
            
            // Create summary table
            var table = SafeMarkup.CreateTable("Property", "Value");
            
            table.AddRow("Name", SafeMarkup.EscapeMarkup(config.Name));
            table.AddRow("Description", SafeMarkup.EscapeMarkup(config.Description ?? "-"));
            table.AddRow("Environment", config.Environment);
            table.AddRow("Version", config.Version);
            table.AddRow("Transaction Mode", config.Options.TransactionMode);
            table.AddRow("Generate Scripts", config.Options.GenerateScripts ? "Yes" : "No");
            
            if (config.Options.GenerateScripts)
            {
                table.AddRow("Script Path", SafeMarkup.EscapeMarkup(config.Options.ScriptOutputPath));
            }
            
            table.AddRow("Command Timeout", $"{config.Options.CommandTimeout} seconds");
            table.AddRow("Create Backup", config.Options.CreateBackup ? "Yes" : "No");
            table.AddRow("Migrations to Run", config.MigrationIds?.Count.ToString() ?? "0");
            
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            
            // Ask for confirmation
            return DialogComponents.ShowConfirmation(
                "Confirm Migration Configuration",
                "Are you ready to proceed with this migration configuration?",
                true);
        }
        
        private async Task<List<MigrationInfo>> LoadAvailableMigrationsAsync()
        {
            // Simulate loading migrations
            await Task.Delay(1000);
            
            // Return dummy data for demo
            return new List<MigrationInfo>
            {
                new MigrationInfo 
                { 
                    Id = 1, 
                    Name = "InitialSchema",
                    Created = DateTime.UtcNow.AddDays(-30),
                    IsApplied = true,
                    AppliedOn = DateTime.UtcNow.AddDays(-29)
                },
                new MigrationInfo 
                { 
                    Id = 2, 
                    Name = "AddUserTable",
                    Created = DateTime.UtcNow.AddDays(-20),
                    IsApplied = true,
                    AppliedOn = DateTime.UtcNow.AddDays(-19)
                },
                new MigrationInfo 
                { 
                    Id = 3, 
                    Name = "AddProductTable",
                    Created = DateTime.UtcNow.AddDays(-10),
                    IsApplied = false
                },
                new MigrationInfo 
                { 
                    Id = 4, 
                    Name = "AddOrderTable",
                    Created = DateTime.UtcNow.AddDays(-5),
                    IsApplied = false
                },
                new MigrationInfo 
                { 
                    Id = 5, 
                    Name = "AddIndexes",
                    Created = DateTime.UtcNow.AddDays(-2),
                    IsApplied = false
                }
            };
        }
    }
    
    /// <summary>
    /// Configuration for database migrations
    /// </summary>
    public class MigrationConfig
    {
        /// <summary>
        /// Migration name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Migration description
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Target environment
        /// </summary>
        public string Environment { get; set; }
        
        /// <summary>
        /// Migration version
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// IDs of migrations to run
        /// </summary>
        public List<int> MigrationIds { get; set; }
        
        /// <summary>
        /// Migration options
        /// </summary>
        public MigrationOptions Options { get; set; }
    }
    
    /// <summary>
    /// Options for database migrations
    /// </summary>
    public class MigrationOptions
    {
        /// <summary>
        /// Transaction mode for migrations
        /// </summary>
        public string TransactionMode { get; set; } = "PerMigration";
        
        /// <summary>
        /// Whether to generate SQL scripts
        /// </summary>
        public bool GenerateScripts { get; set; } = true;
        
        /// <summary>
        /// Output path for generated scripts
        /// </summary>
        public string ScriptOutputPath { get; set; } = "./Scripts";
        
        /// <summary>
        /// Command timeout in seconds
        /// </summary>
        public int CommandTimeout { get; set; } = 60;
        
        /// <summary>
        /// Whether to create a database backup before migrations
        /// </summary>
        public bool CreateBackup { get; set; } = true;
    }
    
    /// <summary>
    /// Information about a database migration
    /// </summary>
    public class MigrationInfo
    {
        /// <summary>
        /// Migration ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Migration name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime Created { get; set; }
        
        /// <summary>
        /// Whether the migration has been applied
        /// </summary>
        public bool IsApplied { get; set; }
        
        /// <summary>
        /// When the migration was applied
        /// </summary>
        public DateTime? AppliedOn { get; set; }
    }
}
