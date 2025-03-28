using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaintainEase.DbMigrator.Contracts.Interfaces.Migrations
{
    /// <summary>
    /// Handles database migrations
    /// </summary>
    public interface IMigrationHandler
    {
        /// <summary>
        /// Gets the provider type this handler supports
        /// </summary>
        string ProviderType { get; }
        
        /// <summary>
        /// Creates a new database migration
        /// </summary>
        Task<MigrationResult> CreateMigrationAsync(MigrationRequest request, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Applies pending migrations
        /// </summary>
        Task<MigrationResult> MigrateAsync(MigrationRequest request, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the migration status
        /// </summary>
        Task<MigrationStatus> GetStatusAsync(MigrationRequest request, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates scripts for pending migrations without applying them
        /// </summary>
        Task<MigrationResult> GenerateScriptsAsync(MigrationRequest request, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Tests the database connection
        /// </summary>
        Task<bool> TestConnectionAsync(MigrationRequest request, CancellationToken cancellationToken = default);
    }
}
