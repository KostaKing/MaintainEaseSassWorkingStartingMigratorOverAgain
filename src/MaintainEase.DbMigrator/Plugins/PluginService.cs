using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Plugins
{
    /// <summary>
    /// Enhanced service for managing database migration plugins
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
            var plugins = _pluginLoader.Plugins;
            _logger.LogInformation("Retrieved {Count} available plugins", plugins.Count);
            return plugins;
        }

        /// <summary>
        /// Gets a plugin by provider type
        /// </summary>
        public IMigrationPlugin GetPlugin(string providerType)
        {
            _logger.LogInformation("Getting plugin for provider type: {ProviderType}", providerType);

            if (string.IsNullOrEmpty(providerType))
            {
                _logger.LogWarning("Provider type is null or empty, using default plugin");
                return _pluginLoader.DefaultPlugin;
            }

            var plugin = _pluginLoader.GetPlugin(providerType);

            if (plugin == null)
            {
                _logger.LogWarning("No plugin found for provider {ProviderType}, using default plugin", providerType);
                return _pluginLoader.DefaultPlugin;
            }

            _logger.LogInformation("Found plugin: {PluginName} for provider {ProviderType}", plugin.Name, providerType);
            return plugin;
        }

        /// <summary>
        /// Gets the default plugin
        /// </summary>
        public IMigrationPlugin GetDefaultPlugin()
        {
            var plugin = _pluginLoader.DefaultPlugin;
            _logger.LogInformation("Retrieved default plugin: {PluginName}", plugin?.Name ?? "None");
            return plugin;
        }

        /// <summary>
        /// Creates a migration
        /// </summary>
        public async Task<MigrationResult> CreateMigrationAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
                _logger.LogInformation("Creating migration using plugin: {PluginName}", plugin.Name);

                var result = await plugin.MigrationHandler.CreateMigrationAsync(request, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully created migration: {MigrationName}", request.MigrationName);
                }
                else
                {
                    _logger.LogWarning("Failed to create migration: {MigrationName}. Error: {ErrorMessage}",
                        request.MigrationName, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating migration: {MigrationName}", request.MigrationName);

                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = $"Error creating migration: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Applies migrations
        /// </summary>
        public async Task<MigrationResult> MigrateAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
                _logger.LogInformation("Applying migrations using plugin: {PluginName}", plugin.Name);

                return await plugin.MigrationHandler.MigrateAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying migrations");

                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = $"Error applying migrations: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets migration status
        /// </summary>
        public async Task<MigrationStatus> GetStatusAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
                _logger.LogInformation("Getting migration status using plugin: {PluginName}", plugin.Name);

                return await plugin.MigrationHandler.GetStatusAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting migration status");

                return new MigrationStatus
                {
                    ErrorMessage = $"Error getting migration status: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Generates migration scripts
        /// </summary>
        public async Task<MigrationResult> GenerateScriptsAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
                _logger.LogInformation("Generating migration scripts using plugin: {PluginName}", plugin.Name);

                return await plugin.MigrationHandler.GenerateScriptsAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating migration scripts");

                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = $"Error generating migration scripts: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        public async Task<bool> TestConnectionAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var plugin = GetPlugin(request.ConnectionConfig.ProviderName);
                _logger.LogInformation("Testing connection using plugin: {PluginName}", plugin.Name);

                return await plugin.MigrationHandler.TestConnectionAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                return false;
            }
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
            // Register plugin loader as singleton since it's expensive to initialize
            services.AddSingleton<PluginLoader>(provider => {
                var logger = provider.GetRequiredService<ILogger<PluginLoader>>();

                // Log initializing message
                logger.LogInformation("Initializing PluginLoader");

                var loader = new PluginLoader(logger);
                loader.Initialize(pluginsPath);

                // Log how many plugins were loaded
                logger.LogInformation("PluginLoader initialized with {Count} plugins", loader.Plugins.Count);

                return loader;
            });

            // Register plugin service as scoped
            services.AddScoped<PluginService>();

            return services;
        }
    }
}
