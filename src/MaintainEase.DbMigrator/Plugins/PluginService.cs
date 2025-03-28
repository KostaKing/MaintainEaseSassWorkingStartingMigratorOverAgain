using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces;

namespace MaintainEase.DbMigrator.Plugins
{
    /// <summary>
    /// Service for managing database migration plugins
    /// </summary>
    public class PluginService
    {
        private readonly PluginLoader _pluginLoader;
        private readonly ILogger<PluginService> _logger;
        
        public PluginService(PluginLoader pluginLoader, ILogger<PluginService> logger)
        {
            _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Gets all available plugins
        /// </summary>
        public IReadOnlyCollection<IMigrationPlugin> GetAvailablePlugins()
        {
            return _pluginLoader.Plugins;
        }
        
        /// <summary>
        /// Gets a plugin by provider type
        /// </summary>
        public IMigrationPlugin GetPlugin(string providerType)
        {
            return _pluginLoader.GetPlugin(providerType);
        }
        
        /// <summary>
        /// Gets the default plugin
        /// </summary>
        public IMigrationPlugin GetDefaultPlugin()
        {
            return _pluginLoader.DefaultPlugin;
        }
        
        /// <summary>
        /// Creates a migration
        /// </summary>
        public async Task<MigrationResult> CreateMigrationAsync(MigrationRequest request)
        {
            var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
            _logger.LogInformation("Creating migration using plugin: {PluginName}", plugin.Name);
            
            return await plugin.MigrationHandler.CreateMigrationAsync(request);
        }
        
        /// <summary>
        /// Applies migrations
        /// </summary>
        public async Task<MigrationResult> MigrateAsync(MigrationRequest request)
        {
            var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
            _logger.LogInformation("Applying migrations using plugin: {PluginName}", plugin.Name);
            
            return await plugin.MigrationHandler.MigrateAsync(request);
        }
        
        /// <summary>
        /// Gets migration status
        /// </summary>
        public async Task<MigrationStatus> GetStatusAsync(MigrationRequest request)
        {
            var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
            _logger.LogInformation("Getting migration status using plugin: {PluginName}", plugin.Name);
            
            return await plugin.MigrationHandler.GetStatusAsync(request);
        }
        
        /// <summary>
        /// Generates migration scripts
        /// </summary>
        public async Task<MigrationResult> GenerateScriptsAsync(MigrationRequest request)
        {
            var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
            _logger.LogInformation("Generating migration scripts using plugin: {PluginName}", plugin.Name);
            
            return await plugin.MigrationHandler.GenerateScriptsAsync(request);
        }
        
        /// <summary>
        /// Tests the database connection
        /// </summary>
        public async Task<bool> TestConnectionAsync(MigrationRequest request)
        {
            var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
            _logger.LogInformation("Testing connection using plugin: {PluginName}", plugin.Name);
            
            return await plugin.MigrationHandler.TestConnectionAsync(request);
        }
    }
    
    /// <summary>
    /// Extension methods for registering plugin services
    /// </summary>
    public static class PluginServiceExtensions
    {
        /// <summary>
        /// Adds migration plugin services to the service collection
        /// </summary>
        public static IServiceCollection AddMigrationPlugins(this IServiceCollection services, string pluginsPath = null)
        {
            // Add plugin loader
            services.AddSingleton<PluginLoader>(provider => {
                var logger = provider.GetRequiredService<ILogger<PluginLoader>>();
                var loader = new PluginLoader(logger);
                loader.Initialize(pluginsPath);
                return loader;
            });
            
            // Add plugin service
            services.AddScoped<PluginService>();
            
            return services;
        }
    }
}
