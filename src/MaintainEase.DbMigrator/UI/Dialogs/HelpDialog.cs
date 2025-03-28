using System;
using System.Collections.Generic;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Components;

namespace MaintainEase.DbMigrator.UI.Dialogs
{
    /// <summary>
    /// Dialog showing help and documentation for the application
    /// </summary>
    public class HelpDialog
    {
        private enum HelpSection
        {
            Overview,
            GettingStarted,
            Migrations,
            DatabaseOperations,
            CommandLine,
            Advanced,
            Troubleshooting
        }

        /// <summary>
        /// Show the help dialog with navigation between sections
        /// </summary>
        public void Show()
        {
            bool exitHelp = false;
            HelpSection currentSection = HelpSection.Overview;

            while (!exitHelp)
            {
                Console.Clear();
                SafeMarkup.SectionHeader("Help & Documentation", color: "blue");
                AnsiConsole.WriteLine();

                // Show the current help section
                ShowHelpSection(currentSection);

                // Show navigation options
                currentSection = ShowHelpNavigation(currentSection);

                // Check if user wants to exit
                if (currentSection == HelpSection.Overview &&
                    MenuComponents.Confirm("Exit help and return to menu?", false))
                {
                    exitHelp = true;
                }
            }
        }

        private void ShowHelpSection(HelpSection section)
        {
            string title = GetSectionTitle(section);
            string content = GetSectionContent(section);

            var helpPanel = new Panel(new Markup(content))
            {
                Header = new PanelHeader($"[bold blue]{title}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2),
                Expand = true
            };

            AnsiConsole.Write(helpPanel);
            AnsiConsole.WriteLine();
        }

        private HelpSection ShowHelpNavigation(HelpSection currentSection)
        {
            var options = new List<string>();
            var sections = Enum.GetValues<HelpSection>();

            // Add "Previous" if not at first section
            if ((int)currentSection > 0)
            {
                options.Add("← Previous Section");
            }

            // Add "Next" if not at last section
            if ((int)currentSection < sections.Length - 1)
            {
                options.Add("→ Next Section");
            }

            // Add return to menu
            options.Add("↩ Return to Help Topics");

            // Show options
            var selection = AnsiConsole.Prompt(
                MenuComponents.CreateSelectionMenu(
                    "Navigation:",
                    options,
                    o => o));

            // Handle selection
            if (selection == "← Previous Section")
            {
                return currentSection - 1;
            }
            else if (selection == "→ Next Section")
            {
                return currentSection + 1;
            }
            else if (selection == "↩ Return to Help Topics")
            {
                // Show topic selection
                return SelectHelpTopic();
            }

            return currentSection;
        }

        private HelpSection SelectHelpTopic()
        {
            var sections = Enum.GetValues<HelpSection>();
            var sectionNames = new Dictionary<HelpSection, string>();

            foreach (var section in sections)
            {
                sectionNames[section] = GetSectionTitle(section);
            }

            return AnsiConsole.Prompt(
                MenuComponents.CreateSelectionMenu(
                    "Select Help Topic:",
                    sections,
                    section => sectionNames[section]));
        }

        private string GetSectionTitle(HelpSection section)
        {
            return section switch
            {
                HelpSection.Overview => "MaintainEase DB Migrator Overview",
                HelpSection.GettingStarted => "Getting Started Guide",
                HelpSection.Migrations => "Working with Migrations",
                HelpSection.DatabaseOperations => "Database Operations Guide",
                HelpSection.CommandLine => "Command Line Interface",
                HelpSection.Advanced => "Advanced Usage and Configuration",
                HelpSection.Troubleshooting => "Troubleshooting & FAQs",
                _ => "Help"
            };
        }

        private string GetSectionContent(HelpSection section)
        {
            return section switch
            {
                HelpSection.Overview => GetOverviewContent(),
                HelpSection.GettingStarted => GetGettingStartedContent(),
                HelpSection.Migrations => GetMigrationsContent(),
                HelpSection.DatabaseOperations => GetDatabaseOperationsContent(),
                HelpSection.CommandLine => GetCommandLineContent(),
                HelpSection.Advanced => GetAdvancedContent(),
                HelpSection.Troubleshooting => GetTroubleshootingContent(),
                _ => "Help content not available."
            };
        }

        private string GetOverviewContent()
        {
            return @"[bold]Welcome to MaintainEase DB Migrator![/]

MaintainEase DB Migrator is a powerful database migration tool designed to make database schema management easy and reliable across development, testing, and production environments.

[bold]Key Features:[/]
• [cyan]Database-agnostic[/] - Works with PostgreSQL, SQL Server, and more
• [cyan]Multi-tenant support[/] - Manage migrations across multiple databases
• [cyan]Automated backups[/] - Keep your data safe with automatic backups
• [cyan]SQL script generation[/] - Generate SQL scripts for review or manual execution
• [cyan]Validation tools[/] - Validate your database schema for common issues
• [cyan]Rich console UI[/] - User-friendly interface with visual feedback
• [cyan]Extensible plugin system[/] - Extend functionality with custom plugins

Whether you're a developer managing local database changes or a DBA deploying to production, MaintainEase makes database migrations safer and more efficient.";
        }

        private string GetGettingStartedContent()
        {
            return @"[bold]Getting Started with MaintainEase[/]

[bold]1. Configure Your Connection[/]
The first step is to configure your database connection:
• From the main menu, select [cyan]Database Operations[/]
• Choose [cyan]Configure Connection[/]
• Follow the wizard to set up your database connection

[bold]2. Create Your First Migration[/]
Once connected, you can create your first migration:
• From the main menu, select [cyan]Manage Database Migrations[/]
• Choose [cyan]Create New Migration[/]
• Name your migration (e.g., ""InitialSchema"")
• Edit the generated migration file in your preferred editor

[bold]3. Run the Migration[/]
After creating and editing your migration:
• From the Migrations menu, select [cyan]Run Migrations[/]
• Follow the wizard to configure migration options
• Confirm to apply the migration to your database

[bold]4. Verify the Changes[/]
After running the migration:
• From the Database menu, select [cyan]View Database Objects[/]
• Verify that your schema changes were applied correctly

[bold]5. Next Steps[/]
Once comfortable with basic migrations, explore more advanced features:
• Creating more complex migrations
• Using the migration wizard for customized options
• Generating SQL scripts for review before execution
• Setting up automated processes with command-line options";
        }

        private string GetMigrationsContent()
        {
            return @"[bold]Working with Migrations[/]

[bold]Migration Types[/]
MaintainEase supports different types of migrations:
• [cyan]Schema migrations[/] - Create, alter, or drop tables, indexes, etc.
• [cyan]Data migrations[/] - Insert, update, or delete data
• [cyan]Composite migrations[/] - Combine schema and data changes

[bold]Creating Migrations[/]
When creating a new migration, you'll need to:
1. Provide a descriptive name (e.g., ""AddUserTable"", ""UpdateProductPrices"")
2. Edit the migration file, which contains:
   • An [cyan]Up()[/] method for applying changes
   • A [cyan]Down()[/] method for rolling back changes

[bold]Migration Best Practices[/]
• Keep migrations small and focused on specific changes
• Always implement the Down() method for proper rollbacks
• Use transactions to ensure atomic changes
• Test migrations thoroughly in development before applying to production
• Create a backup before applying migrations to production

[bold]Migration Ordering[/]
Migrations are applied in order based on:
1. Timestamp prefix in the filename
2. Dependencies between migrations

[bold]Handling Migration Failures[/]
If a migration fails:
1. Check the error message and logs for details
2. Fix the issue in the migration file
3. Either retry the migration or roll back and fix the entire migration

[bold]Validating Migrations[/]
Before applying migrations to production:
• Use the [cyan]Validate Database[/] option to check for potential issues
• Generate and review SQL scripts
• Consider running migrations in a staging environment first";
        }

        private string GetDatabaseOperationsContent()
        {
            return @"[bold]Database Operations Guide[/]

[bold]Viewing Database Structure[/]
MaintainEase provides several ways to explore your database:
• View tables, views, procedures, and other objects
• Explore table structures with columns and indexes
• Check constraints, relationships, and triggers

[bold]Backup and Restore[/]
Protect your data with built-in backup features:
• Create full database backups before migrations
• Schedule regular automated backups
• Restore from backup in case of issues
• Configure backup retention policies

[bold]Database Comparison[/]
Compare databases to identify differences:
• Compare schema between two databases
• Identify missing or different objects
• Generate scripts to synchronize databases

[bold]Performance Optimization[/]
Identify and resolve performance issues:
• Analyze index usage and missing indexes
• View query performance statistics
• Optimize database configuration

[bold]Multi-Tenant Operations[/]
For applications with multiple tenants:
• Manage migrations across all tenants
• Apply changes to specific tenants
• Compare tenant databases for consistency

[bold]Database Monitoring[/]
Keep track of database health and performance:
• Monitor size and growth
• Track long-running queries
• Set up alerts for potential issues

[bold]Security Management[/]
Manage database security effectively:
• View and configure user permissions
• Audit access and changes
• Implement security best practices";
        }

        private string GetCommandLineContent()
        {
            return @"[bold]Command Line Interface[/]

MaintainEase provides a comprehensive command-line interface for automation and integration with CI/CD pipelines.

[bold]Basic Usage[/]
```
MaintainEase.DbMigrator [command] [options]
```

[bold]Common Commands[/]

[bold]migrate[/] - Apply pending migrations
```
MaintainEase.DbMigrator migrate --environment Production
```

[bold]rollback[/] - Rollback migrations
```
MaintainEase.DbMigrator rollback --steps 1
```

[bold]create[/] - Create a new migration
```
MaintainEase.DbMigrator create --name AddUserTable
```

[bold]script[/] - Generate SQL scripts
```
MaintainEase.DbMigrator script --output ./scripts
```

[bold]validate[/] - Validate database
```
MaintainEase.DbMigrator validate --connection ""connection-string""
```

[bold]status[/] - Show migration status
```
MaintainEase.DbMigrator status
```

[bold]Global Options[/]
--connection ""connection-string""  Specify database connection
--environment Environment           Target environment
--tenant TenantName                 Specify tenant
--verbose                          Enable verbose output
--no-prompt                        Skip confirmation prompts
--config path/to/config.json       Use specific config file

[bold]Automation Examples[/]
[bold]CI/CD Pipeline:[/]
```
MaintainEase.DbMigrator migrate --environment Staging --no-prompt --verbose
```

[bold]Scheduled Database Backup:[/]
```
MaintainEase.DbMigrator backup --name ""nightly-backup"" --retention 7
```

[bold]Database Health Check:[/]
```
MaintainEase.DbMigrator validate --output report.json
```";
        }

        private string GetAdvancedContent()
        {
            return @"[bold]Advanced Usage and Configuration[/]

[bold]Custom Configuration Files[/]
Create environment-specific configuration files:
• development.settings.json
• staging.settings.json
• production.settings.json

[bold]Migration Customization[/]
For complex migration scenarios:
• Use custom migration providers
• Implement custom version resolvers
• Create migration templates for specific needs

[bold]Extending with Plugins[/]
MaintainEase supports plugins for extended functionality:
• Custom database providers
• Additional verification tools
• Integration with external services
• Custom reporting tools

To install plugins:
1. Place plugin DLLs in the Plugins directory
2. Configure through the Settings menu
3. Restart the application

[bold]Multi-Environment Workflows[/]
Configure efficient workflows across environments:
• Development → Testing → Staging → Production
• Environment-specific configuration
• Migration verification at each stage

[bold]Advanced Transaction Control[/]
Fine-tune transaction behavior:
• Per-migration transactions
• Custom savepoints
• Transaction timeout configuration
• Error handling strategies

[bold]Performance Tuning[/]
For large databases:
• Batch processing for data migrations
• Optimized index creation
• Parallel execution where safe
• Memory and timeout configuration

[bold]Scripting and Automation[/]
Integrate with external tools:
• PowerShell integration
• Bash script integration
• CI/CD pipeline configuration
• Scheduled task automation";
        }

        private string GetTroubleshootingContent()
        {
            return @"[bold]Troubleshooting & FAQs[/]

[bold]Common Issues and Solutions[/]

[bold]Issue:[/] Migration fails with ""connection refused""
[bold]Solution:[/]
• Verify connection string is correct
• Check database server is running
• Ensure firewall allows the connection
• Verify network connectivity

[bold]Issue:[/] ""Permission denied"" errors
[bold]Solution:[/]
• Check user permissions in the database
• Ensure the user has CREATE/ALTER/DROP privileges
• For SQL Server, check if user is db_owner
• For PostgreSQL, check role permissions

[bold]Issue:[/] Migration timeout
[bold]Solution:[/]
• Increase command timeout in settings
• Break down large migrations into smaller ones
• Check for locks or blocking operations
• Run at off-peak hours for production

[bold]Issue:[/] Version conflicts
[bold]Solution:[/]
• Check for duplicate migration versions
• Ensure migrations are applied in correct order
• Resolve merge conflicts in migration files
• Re-establish baseline if necessary

[bold]Issue:[/] Data loss during migration
[bold]Solution:[/]
• Always enable automatic backups
• Test migrations in non-production first
• Use transactions for atomic operations
• Implement proper Down() methods

[bold]Diagnostic Tools[/]
• Enable verbose logging in settings
• Check log files in the Logs directory
• Use the validate command to identify issues
• Analyze generated SQL scripts before applying

[bold]Getting Help[/]
If you encounter problems not covered here:
• Check documentation for updates
• Search issues on GitHub repository
• Join the community forum
• Contact support with detailed error logs";
        }
    }
}
