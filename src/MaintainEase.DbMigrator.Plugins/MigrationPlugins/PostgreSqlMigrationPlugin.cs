using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Plugins.MigrationPlugins.Handlers;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Plugins.MigrationPlugins
{
    /// <summary>
    /// PostgreSQL migration plugin
    /// </summary>
    public class PostgreSqlMigrationPlugin : IMigrationPlugin
    {
        private readonly IMigrationHandler _migrationHandler;

        public PostgreSqlMigrationPlugin()
        {
            _migrationHandler = new PostgreSqlMigrationHandler();
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
        public string Description => "Provides PostgreSQL database migration capabilities";

        /// <summary>
        /// Gets the capabilities supported by this plugin
        /// </summary>
        public IEnumerable<string> Capabilities => new[]
        {
            "Migration",
            "Script Generation",
            "Status Check",
            "Backup"
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
