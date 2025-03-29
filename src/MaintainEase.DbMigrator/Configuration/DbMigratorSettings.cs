using System;
using System.Collections.Generic;

namespace MaintainEase.DbMigrator.Configuration
{
    /// <summary>
    /// Configuration settings for the DbMigrator
    /// </summary>
    public class DbMigratorSettings
    {
        /// <summary>
        /// Default connection string to use if no tenant-specific connection is defined
        /// </summary>
        public string DefaultConnectionString { get; set; }

        /// <summary>
        /// Base connection string for core migrations
        /// </summary>
        public string BaseConnectionString { get; set; }

        /// <summary>
        /// Default database provider to use (SqlServer or PostgreSQL)
        /// </summary>
        public string DatabaseProvider { get; set; } = "SqlServer";

        /// <summary>
        /// Path where migrations should be stored
        /// </summary>
        public string MigrationsPath { get; set; } = "Migrations";

        /// <summary>
        /// Whether multi-tenancy is enabled
        /// </summary>
        public bool EnableMultiTenancy { get; set; } = true;

        /// <summary>
        /// List of tenants
        /// </summary>
        public List<TenantSettings> Tenants { get; set; } = new List<TenantSettings>();

        /// <summary>
        /// Backup settings
        /// </summary>
        public BackupSettings Backup { get; set; } = new BackupSettings();

        /// <summary>
        /// Console UI settings
        /// </summary>
        public ConsoleSettings Console { get; set; } = new ConsoleSettings();

        /// <summary>
        /// Logging settings
        /// </summary>
        public LoggingSettings Logging { get; set; } = new LoggingSettings();

        /// <summary>
        /// Provider-specific settings
        /// </summary>
        public ProviderSettings Providers { get; set; } = new ProviderSettings();
    }

    /// <summary>
    /// Settings for a specific tenant
    /// </summary>
    public class TenantSettings
    {
        /// <summary>
        /// Unique identifier for the tenant
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Display name for the tenant
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tenant-specific connection string (if empty, DefaultConnectionString is used)
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Whether migrations are enabled for this tenant
        /// </summary>
        public bool EnableMigrations { get; set; } = true;
    }

    /// <summary>
    /// Backup settings
    /// </summary>
    public class BackupSettings
    {
        /// <summary>
        /// Path where backups should be stored
        /// </summary>
        public string BackupPath { get; set; } = "Backups";

        /// <summary>
        /// Number of backups to keep
        /// </summary>
        public int BackupsToKeep { get; set; } = 5;

        /// <summary>
        /// Whether to create a backup before migration
        /// </summary>
        public bool BackupBeforeMigration { get; set; } = true;
    }

    /// <summary>
    /// Console UI settings
    /// </summary>
    public class ConsoleSettings
    {
        /// <summary>
        /// Whether to use fancy UI elements
        /// </summary>
        public bool EnableFancyUI { get; set; } = true;

        /// <summary>
        /// Whether to show verbose output
        /// </summary>
        public bool VerboseOutput { get; set; } = false;

        /// <summary>
        /// UI theme to use
        /// </summary>
        public string Theme { get; set; } = "Default";
    }

    /// <summary>
    /// Logging settings
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Log level
        /// </summary>
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// Path where logs should be stored
        /// </summary>
        public string LogPath { get; set; } = "Logs";

        /// <summary>
        /// Whether to log migration operations
        /// </summary>
        public bool LogMigrationOperations { get; set; } = true;
    }

    /// <summary>
    /// Provider-specific settings
    /// </summary>
    public class ProviderSettings
    {
        /// <summary>
        /// SQL Server specific settings
        /// </summary>
        public DatabaseProviderSettings SqlServer { get; set; } = new DatabaseProviderSettings
        {
            MigrationHistoryTable = "__EFMigrationsHistory"
        };

        /// <summary>
        /// PostgreSQL specific settings
        /// </summary>
        public PostgreSqlSettings PostgreSQL { get; set; } = new PostgreSqlSettings
        {
            MigrationHistoryTable = "__EFMigrationsHistory",
            Schema = "public"
        };
    }

    /// <summary>
    /// Base database provider settings
    /// </summary>
    public class DatabaseProviderSettings
    {
        /// <summary>
        /// Name of the migrations history table
        /// </summary>
        public string MigrationHistoryTable { get; set; } = "__EFMigrationsHistory";
    }

    /// <summary>
    /// PostgreSQL specific settings
    /// </summary>
    public class PostgreSqlSettings : DatabaseProviderSettings
    {
        /// <summary>
        /// PostgreSQL schema to use
        /// </summary>
        public string Schema { get; set; } = "public";
    }
}
