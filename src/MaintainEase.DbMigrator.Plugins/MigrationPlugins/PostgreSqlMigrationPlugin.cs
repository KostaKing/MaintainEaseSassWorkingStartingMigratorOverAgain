using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Plugins.MigrationPlugins
{
    /// <summary>
    /// PostgreSQL migration plugin
    /// </summary>
    public class PostgreSqlMigrationPlugin : IMigrationPlugin
    {
        private readonly IMigrationHandler _migrationHandler;

        /// <summary>
        /// Parameterless constructor - required for plugin discovery
        /// </summary>
        public PostgreSqlMigrationPlugin()
        {
            // Create handler without logger
            _migrationHandler = new Handlers.PostgreSqlMigrationHandler();
        }

        /// <summary>
        /// Constructor with logger
        /// </summary>
        public PostgreSqlMigrationPlugin(ILogger<Handlers.PostgreSqlMigrationHandler> logger)
        {
            _migrationHandler = new Handlers.PostgreSqlMigrationHandler(logger);
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        public string Name => "PostgreSQL Migration Plugin";

        /// <summary>
        /// Gets the provider type of the plugin
        /// </summary>
        public string ProviderType => "PostgreSQL";

        /// <summary>
        /// Gets the version of the plugin
        /// </summary>
        public string Version => "1.0.0";

        /// <summary>
        /// Gets the description of the plugin
        /// </summary>
        public string Description => "Provides migration capabilities for PostgreSQL databases";

        /// <summary>
        /// Gets the capabilities supported by this plugin
        /// </summary>
        public IEnumerable<string> Capabilities => new[]
        {
            "Create Migration",
            "Apply Migration",
            "Generate Scripts",
            "Check Status",
            "Test Connection"
        };

        /// <summary>
        /// Gets a value indicating whether this plugin is the default
        /// </summary>
        public bool IsDefault => false;

        /// <summary>
        /// Gets the migration handler
        /// </summary>
        public IMigrationHandler MigrationHandler => _migrationHandler;
    }
}
