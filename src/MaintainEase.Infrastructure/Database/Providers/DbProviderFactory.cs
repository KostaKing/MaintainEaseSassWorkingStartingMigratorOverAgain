using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MaintainEase.Infrastructure.Database.Providers
{
    /// <summary>
    /// Factory for creating and managing database providers
    /// </summary>
    public class DbProviderFactory : IDbProviderFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IDbProvider _postgresProvider;
        private readonly IDbProvider _sqlServerProvider;

        public DbProviderFactory(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _postgresProvider = new PostgreSqlProvider();
            _sqlServerProvider = new SqlServerProvider();
        }

        /// <summary>
        /// Get the appropriate provider based on configuration
        /// </summary>
        public IDbProvider GetProvider()
        {
            var providerName = _configuration["Database:Provider"];
            
            return providerName?.ToLowerInvariant() switch
            {
                "sqlserver" => _sqlServerProvider,
                "mssql" => _sqlServerProvider,
                _ => _postgresProvider // Default to PostgreSQL
            };
        }

        /// <summary>
        /// Get a specific provider by name
        /// </summary>
        public IDbProvider GetProvider(string providerName)
        {
            return providerName?.ToLowerInvariant() switch
            {
                "sqlserver" => _sqlServerProvider,
                "mssql" => _sqlServerProvider,
                "postgresql" => _postgresProvider,
                "npgsql" => _postgresProvider,
                _ => throw new ArgumentException($"Unknown database provider: {providerName}")
            };
        }

        /// <summary>
        /// Configure the database context with the appropriate provider
        /// </summary>
        public void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            var provider = GetProvider();
            provider.ConfigureDbContext(optionsBuilder, connectionString);
        }
    }

    /// <summary>
    /// Interface for database provider factory
    /// </summary>
    public interface IDbProviderFactory
    {
        IDbProvider GetProvider();
        IDbProvider GetProvider(string providerName);
        void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder, string connectionString);
    }
}
