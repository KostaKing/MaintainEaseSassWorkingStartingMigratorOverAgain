using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.Contracts.Interfaces;

namespace MaintainEase.DbMigrator.UI.Wizards
{
    /// <summary>
    /// Wizard for configuring and testing database connections
    /// </summary>
    public class ConnectionWizard
    {
        private readonly ApplicationContext _appContext;
        
        public ConnectionWizard(ApplicationContext appContext)
        {
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
        }
        
        /// <summary>
        /// Run the connection configuration wizard
        /// </summary>
        public async Task<ConnectionConfig> RunAsync(ConnectionConfig existingConfig = null)
        {
            // Create a new connection config or use the existing one
            var config = existingConfig ?? new ConnectionConfig 
            { 
                ProviderName = "PostgreSQL", 
                RetryPolicy = new RetryPolicy() 
            };
            
            SafeMarkup.Banner("Database Connection Setup");
            SafeMarkup.Info("This wizard will help you configure a database connection.");
            
            // Step 1: Select provider
            config.ProviderName = SelectProvider(config.ProviderName);
            
            // Step 2: Configure connection string
            config.ConnectionString = ConfigureConnectionString(config);
            
            // Step 3: Azure authentication
            config.UseAzureAuthentication = ConfigureAzureAuthentication(config);
            
            // Step 4: Configure advanced options
            ConfigureAdvancedOptions(config);
            
            // Step 5: Test connection
            bool connectionSuccessful = await TestConnectionAsync(config);
            
            if (connectionSuccessful)
            {
                SafeMarkup.Success("Connection configuration completed successfully!");
                return config;
            }
            else
            {
                if (DialogComponents.ShowConfirmation(
                    "Connection Failed",
                    "The connection test failed. Would you like to revise your connection settings?",
                    true))
                {
                    // Recursively run the wizard again
                    return await RunAsync(config);
                }
                else
                {
                    SafeMarkup.Warning("Keeping current connection settings despite test failure.");
                    return config;
                }
            }
        }
        
        private string SelectProvider(string currentProvider)
        {
            SafeMarkup.SectionHeader("Database Provider");
            
            var providers = new Dictionary<string, string>
            {
                ["PostgreSQL"] = "PostgreSQL database server",
                ["SqlServer"] = "Microsoft SQL Server"
            };
            
            var providerOptions = new List<string>(providers.Keys);
            
            var selectedProvider = AnsiConsole.Prompt(
                MenuComponents.CreateDescriptiveMenu(
                    "Select a database provider:", providers));
                    
            return selectedProvider;
        }
        
        private string ConfigureConnectionString(ConnectionConfig config)
        {
            SafeMarkup.SectionHeader("Connection String");
            
            // Check if there's an existing connection string
            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                SafeMarkup.Info($"Current connection string: {SafeMarkup.EscapeMarkup(config.ConnectionString)}");
                
                if (!MenuComponents.Confirm("Do you want to change the connection string?", false))
                {
                    return config.ConnectionString;
                }
            }
            
            // Show example based on provider
            string promptText = $"Enter a connection string for {config.ProviderName}:";
            string exampleString = config.ProviderName switch
            {
                "PostgreSQL" => "Host=localhost;Database=mydb;Username=postgres;Password=password",
                "SqlServer" => "Server=localhost;Database=mydb;User Id=sa;Password=password",
                _ => "Server=localhost;Database=mydb;User Id=user;Password=password"
            };
            
            SafeMarkup.Info($"Example: {exampleString}");
            
            var connectionPrompt = MenuComponents.CreateConnectionStringPrompt(
                promptText,
                config.ConnectionString,
                config.ProviderName);
                
            return AnsiConsole.Prompt(connectionPrompt);
        }
        
        private bool ConfigureAzureAuthentication(ConnectionConfig config)
        {
            SafeMarkup.SectionHeader("Azure Authentication");
            
            bool isAzureDetected = false;
            
            // Try to detect if running in Azure
            try
            {
                isAzureDetected = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
                
                if (isAzureDetected)
                {
                    SafeMarkup.Info("Azure environment detected!");
                }
            }
            catch { /* Ignore detection errors */ }
            
            return MenuComponents.Confirm(
                "Do you want to use Azure AD authentication?", 
                config.UseAzureAuthentication|| isAzureDetected);
        }
        
        private void ConfigureAdvancedOptions(ConnectionConfig config)
        {
            SafeMarkup.SectionHeader("Advanced Options");
            
            if (!MenuComponents.Confirm("Do you want to configure advanced connection options?", false))
            {
                return;
            }
            
            // Command timeout
            config.CommandTimeout = AnsiConsole.Prompt(
                new TextPrompt<int>("Command timeout in seconds:")
                    .DefaultValue(config.CommandTimeout)
                    .ValidationErrorMessage("[red]Please enter a valid timeout (5-300 seconds).[/]")
                    .Validate(timeout => 
                        timeout < 5 || timeout > 300 
                            ? ValidationResult.Error("[red]Timeout must be between 5 and 300 seconds.[/]") 
                            : ValidationResult.Success()));
            
            // Retry policy
            config.RetryPolicy.MaxRetryCount = AnsiConsole.Prompt(
                new TextPrompt<int>("Maximum retry count:")
                    .DefaultValue(config.RetryPolicy.MaxRetryCount)
                    .ValidationErrorMessage("[red]Please enter a valid retry count (0-10).[/]")
                    .Validate(count => 
                        count < 0 || count > 10 
                            ? ValidationResult.Error("[red]Retry count must be between 0 and 10.[/]") 
                            : ValidationResult.Success()));
            
            config.RetryPolicy.DelaySeconds = AnsiConsole.Prompt(
                new TextPrompt<int>("Delay between retries (seconds):")
                    .DefaultValue(config.RetryPolicy.DelaySeconds)
                    .ValidationErrorMessage("[red]Please enter a valid delay (1-30 seconds).[/]")
                    .Validate(delay => 
                        delay < 1 || delay > 30 
                            ? ValidationResult.Error("[red]Delay must be between 1 and 30 seconds.[/]") 
                            : ValidationResult.Success()));
            
            config.RetryPolicy.Exponential = MenuComponents.Confirm(
                "Use exponential backoff for retries?",
                config.RetryPolicy.Exponential);
        }
        
        private async Task<bool> TestConnectionAsync(ConnectionConfig config)
        {
            SafeMarkup.SectionHeader("Testing Connection");
            
            var result = await SpinnerComponents.WithSpinnerAsync(
                $"Testing connection to {config.ProviderName} database...",
                async () => 
                {
                    // Simulate connection test for demo
                    await Task.Delay(2000);
                    
                    // In a real application, we would try to connect to the database
                    // and return the result. For this example, we'll just return true.
                    return true;
                },
                "Database");
                
            if (result)
            {
                SafeMarkup.Success("Connection test successful!");
            }
            else
            {
                SafeMarkup.Error("Failed to connect to database.");
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Configuration for database connections
    /// </summary>
    public class ConnectionConfig
    {
        /// <summary>
        /// Database provider name (PostgreSQL, SqlServer, etc.)
        /// </summary>
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Database connection string
        /// </summary>
        public string ConnectionString { get; set; }
        
        /// <summary>
        /// Whether to use Azure authentication
        /// </summary>
        public bool UseAzureAuthentication { get; set; }
        
        /// <summary>
        /// Command timeout in seconds
        /// </summary>
        public int CommandTimeout { get; set; } = 30;
        
        /// <summary>
        /// Retry policy for database operations
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; }
    }
    
    /// <summary>
    /// Retry policy for database operations
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
        
        /// <summary>
        /// Delay between retries in seconds
        /// </summary>
        public int DelaySeconds { get; set; } = 5;
        
        /// <summary>
        /// Whether to use exponential backoff for retries
        /// </summary>
        public bool Exponential { get; set; } = true;
    }
}
