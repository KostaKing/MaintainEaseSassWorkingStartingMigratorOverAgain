using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Plugins.SqlServer.Handlers;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Plugins.SqlServer
{
    /// <summary>
    /// SQL Server migration plugin
    /// </summary>
    public class SqlServerMigrationPlugin : IMigrationPlugin
    {
        private readonly IMigrationHandler _migrationHandler;
        
        public SqlServerMigrationPlugin()
        {
            _migrationHandler = new SqlServerMigrationHandler();
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
        public string Description => "Provides SQL Server database migration capabilities";
        
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
        public bool IsDefault => true;
        
        /// <summary>
        /// Gets the migration handler
        /// </summary>
        public IMigrationHandler MigrationHandler => _migrationHandler;
    }
}
