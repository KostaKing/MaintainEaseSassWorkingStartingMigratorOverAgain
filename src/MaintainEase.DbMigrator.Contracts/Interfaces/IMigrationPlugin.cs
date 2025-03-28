using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaintainEase.DbMigrator.Contracts.Interfaces
{
    /// <summary>
    /// Represents a database provider plugin
    /// </summary>
    public interface IMigrationPlugin
    {
        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the provider type of the plugin (e.g., "SqlServer", "PostgreSQL")
        /// </summary>
        string ProviderType { get; }
        
        /// <summary>
        /// Gets the version of the plugin
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// Gets the description of the plugin
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Gets the capabilities supported by this plugin
        /// </summary>
        IEnumerable<string> Capabilities { get; }
        
        /// <summary>
        /// Gets a value indicating whether this plugin is the default
        /// </summary>
        bool IsDefault { get; }
        
        /// <summary>
        /// Gets the migration handler
        /// </summary>
        IMigrationHandler MigrationHandler { get; }
    }
}
