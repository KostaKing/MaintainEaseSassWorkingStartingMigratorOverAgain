using System;
using MaintainEase.DbMigrator.Contracts.Interfaces;
using MaintainEase.DbMigrator.UI.Wizards;

namespace MaintainEase.DbMigrator.Plugins
{
    /// <summary>
    /// Configuration for database connections
    /// </summary>
    public class ConnectionConfig : IConnectionConfig
    {
        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the provider name
        /// </summary>
        public string ProviderName { get; set; } = "SqlServer";

        /// <summary>
        /// Gets or sets the timeout in seconds
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether to use a transaction
        /// </summary>
        public bool UseTransaction { get; set; } = true;

        /// <summary>
        /// Creates a new connection configuration
        /// </summary>
        public ConnectionConfig()
        {
        }

        /// <summary>
        /// Creates a new connection configuration with the specified connection string
        /// </summary>
        public ConnectionConfig(string connectionString, string providerName = "SqlServer")
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        }
    }
}
