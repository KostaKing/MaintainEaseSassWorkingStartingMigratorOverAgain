using System;
using Microsoft.EntityFrameworkCore;

namespace MaintainEase.Infrastructure.Database.Providers
{
    /// <summary>
    /// SQL Server database provider implementation
    /// </summary>
    public class SqlServerProvider : IDbProvider
    {
        public string Name => "SqlServer";

        public void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            {
                // Enable retry on failure
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        }
    }
}
