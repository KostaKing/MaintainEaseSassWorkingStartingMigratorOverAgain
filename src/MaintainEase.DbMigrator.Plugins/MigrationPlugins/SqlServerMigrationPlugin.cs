using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Plugins.MigrationPlugins
{
    /// <summary>
    /// SQL Server migration plugin
    /// </summary>
    public class SqlServerMigrationPlugin : IMigrationPlugin
    {
        private readonly IMigrationHandler _migrationHandler;

        /// <summary>
        /// Parameterless constructor - required for plugin discovery
        /// </summary>
        public SqlServerMigrationPlugin()
        {
            // Create handler without logger
            _migrationHandler = new Handlers.SqlServerMigrationHandler();
        }

        /// <summary>
        /// Constructor with logger
        /// </summary>
        public SqlServerMigrationPlugin(ILogger<Handlers.SqlServerMigrationHandler> logger)
        {
            _migrationHandler = new Handlers.SqlServerMigrationHandler(logger);
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        public string Name => "SQL Server Migration Plugin";

        /// <summary>
        /// Gets the provider type of the plugin
        /// </summary>
        public string ProviderType => "SqlServer";

        /// <summary>
        /// Gets the version of the plugin
        /// </summary>
        public string Version => "1.0.0";

        /// <summary>
        /// Gets the description of the plugin
        /// </summary>
        public string Description => "Provides migration capabilities for SQL Server databases using EF Core";

        /// <summary>
        /// Gets the capabilities supported by this plugin
        /// </summary>
        public IEnumerable<string> Capabilities => new[]
        {
            "Create Migration",
            "Apply Migration",
            "Generate Scripts",
            "Check Status",
            "Test Connection",
            "Backup Database"
        };

        /// <summary>
        /// Gets a value indicating whether this plugin is the default
        /// </summary>
        public bool IsDefault => true;

        /// <summary>
        /// Gets the migration handler
        /// </summary>
        public IMigrationHandler MigrationHandler => _migrationHandler;
    }
}
