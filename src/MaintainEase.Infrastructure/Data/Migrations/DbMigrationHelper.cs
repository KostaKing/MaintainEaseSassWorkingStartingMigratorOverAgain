using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaintainEase.Infrastructure.Database.Providers;
using MaintainEase.Infrastructure.MultiTenancy;
using MaintainEase.Infrastructure.Data.Context;

namespace MaintainEase.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Helper for database migrations that can be used by DbMigrator
    /// </summary>
    public class DbMigrationHelper : IDbMigrationHelper
    {
        private readonly IDbProviderFactory _dbProviderFactory;
        private readonly ITenantResolver _tenantResolver;
        private readonly ILogger<DbMigrationHelper> _logger;

        public DbMigrationHelper(
            IDbProviderFactory dbProviderFactory,
            ITenantResolver tenantResolver,
            ILogger<DbMigrationHelper> logger)
        {
            _dbProviderFactory = dbProviderFactory ?? throw new ArgumentNullException(nameof(dbProviderFactory));
            _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Migrate a specific tenant's database
        /// </summary>
        public async Task MigrateTenantDatabaseAsync(string tenantIdentifier, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Migrating database for tenant {TenantIdentifier}", tenantIdentifier);

            try
            {
                // Get tenant connection string
                var connectionString = _tenantResolver.ResolveTenantConnectionString(tenantIdentifier);

                // Create options builder
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                _dbProviderFactory.ConfigureDbContext(optionsBuilder, connectionString);

                // Create context with minimal dependencies for migration only
                using var context = new MigrationAppDbContext(optionsBuilder.Options);

                // Apply migrations
                await context.Database.MigrateAsync(cancellationToken);

                _logger.LogInformation("Successfully migrated database for tenant {TenantIdentifier}", tenantIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating database for tenant {TenantIdentifier}", tenantIdentifier);
                throw;
            }
        }

        /// <summary>
        /// Check if migrations need to be applied for a tenant
        /// </summary>
        public async Task<bool> TenantDatabaseNeedsMigrationAsync(string tenantIdentifier, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get tenant connection string
                var connectionString = _tenantResolver.ResolveTenantConnectionString(tenantIdentifier);

                // Create options builder
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                _dbProviderFactory.ConfigureDbContext(optionsBuilder, connectionString);

                // Create context with minimal dependencies for migration only
                using var context = new MigrationAppDbContext(optionsBuilder.Options);

                // Check if migrations are pending
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
                return pendingMigrations.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking migrations for tenant {TenantIdentifier}", tenantIdentifier);
                return false;
            }
        }
    }

    /// <summary>
    /// Interface for database migration helper
    /// </summary>
    public interface IDbMigrationHelper
    {
        Task MigrateTenantDatabaseAsync(string tenantIdentifier, CancellationToken cancellationToken = default);
        Task<bool> TenantDatabaseNeedsMigrationAsync(string tenantIdentifier, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Minimal DbContext for migrations only
    /// </summary>
    internal class MigrationAppDbContext : AppDbContext
    {
        public MigrationAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options,
                  null,    // ITenantProvider 
                  null,    // IAuditInterceptor 
                  null)    // IDomainEventInterceptor
        {
        }

        // Override to disable functionality not needed for migrations
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Skip base implementation since options are already configured
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply minimal model configuration needed for migrations
            base.OnModelCreating(modelBuilder);
        }
    }
}
