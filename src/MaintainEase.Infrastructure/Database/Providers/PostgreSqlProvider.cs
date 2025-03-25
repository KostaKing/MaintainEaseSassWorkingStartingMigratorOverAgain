using System;
using Microsoft.EntityFrameworkCore;

namespace MaintainEase.Infrastructure.Database.Providers
{
    /// <summary>
    /// PostgreSQL database provider implementation
    /// </summary>
    public class PostgreSqlProvider : IDbProvider
    {
        public string Name => "PostgreSQL";

        public void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Configure migrations history table in public schema
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                
                // Enable retry on failure
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
        }
    }
}
