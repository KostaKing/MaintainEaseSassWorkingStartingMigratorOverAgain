using System;
using System.Collections.Generic;

namespace MaintainEase.DbMigrator.Contracts.Interfaces
{
    /// <summary>
    /// Represents a configuration for database connections
    /// </summary>
    public interface IConnectionConfig
    {
        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        string ConnectionString { get; set; }
        
        /// <summary>
        /// Gets or sets the provider name
        /// </summary>
        string ProviderName { get; set; }
        
        /// <summary>
        /// Gets or sets the timeout in seconds
        /// </summary>
        int Timeout { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to use a transaction
        /// </summary>
        bool UseTransaction { get; set; }
    }
    
    /// <summary>
    /// Request information for a migration operation
    /// </summary>
    public class MigrationRequest
    {
        /// <summary>
        /// Gets or sets the connection configuration
        /// </summary>
        public IConnectionConfig ConnectionConfig { get; set; }
        
        /// <summary>
        /// Gets or sets the migration name
        /// </summary>
        public string MigrationName { get; set; }
        
        /// <summary>
        /// Gets or sets the output directory for scripts
        /// </summary>
        public string OutputDirectory { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to create a backup before migrating
        /// </summary>
        public bool CreateBackup { get; set; }
        
        /// <summary>
        /// Gets or sets the tenant identifier
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// Gets or sets the environment name
        /// </summary>
        public string Environment { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to execute in verbose mode
        /// </summary>
        public bool Verbose { get; set; }
    }
    
    /// <summary>
    /// Result of a migration operation
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Gets or sets the error message if the operation failed
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the migrations that were applied or would be applied
        /// </summary>
        public List<MigrationInfo> AppliedMigrations { get; set; } = new List<MigrationInfo>();
        
        /// <summary>
        /// Gets or sets the path to the generated scripts
        /// </summary>
        public string ScriptsPath { get; set; }
        
        /// <summary>
        /// Gets or sets the path to the backup
        /// </summary>
        public string BackupPath { get; set; }
        
        /// <summary>
        /// Gets or sets additional information about the operation
        /// </summary>
        public Dictionary<string, string> AdditionalInfo { get; set; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Information about a migration
    /// </summary>
    public class MigrationInfo
    {
        /// <summary>
        /// Gets or sets the migration identifier
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the migration name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets when the migration was applied
        /// </summary>
        public DateTime? AppliedOn { get; set; }
        
        /// <summary>
        /// Gets or sets the script that would apply this migration
        /// </summary>
        public string Script { get; set; }
    }
    
    /// <summary>
    /// Status of the database migrations
    /// </summary>
    public class MigrationStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether there are pending migrations
        /// </summary>
        public bool HasPendingMigrations { get; set; }
        
        /// <summary>
        /// Gets or sets the number of pending migrations
        /// </summary>
        public int PendingMigrationsCount { get; set; }
        
        /// <summary>
        /// Gets or sets the pending migrations
        /// </summary>
        public List<MigrationInfo> PendingMigrations { get; set; } = new List<MigrationInfo>();
        
        /// <summary>
        /// Gets or sets the applied migrations
        /// </summary>
        public List<MigrationInfo> AppliedMigrations { get; set; } = new List<MigrationInfo>();
        
        /// <summary>
        /// Gets or sets when the last migration was applied
        /// </summary>
        public DateTime? LastMigrationDate { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the last migration
        /// </summary>
        public string LastMigrationName { get; set; }
        
        /// <summary>
        /// Gets or sets the provider name
        /// </summary>
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Gets or sets the database name
        /// </summary>
        public string DatabaseName { get; set; }
        
        /// <summary>
        /// Gets or sets the database version
        /// </summary>
        public string DatabaseVersion { get; set; }
    }
}
