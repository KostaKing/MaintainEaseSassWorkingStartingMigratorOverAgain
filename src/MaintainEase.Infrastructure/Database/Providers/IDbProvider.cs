using System;
using Microsoft.EntityFrameworkCore;

namespace MaintainEase.Infrastructure.Database.Providers
{
    /// <summary>
    /// Interface for database provider configurations
    /// </summary>
    public interface IDbProvider
    {
        /// <summary>
        /// Get the provider name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Configure the database context options
        /// </summary>
        void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder, string connectionString);
    }
}
