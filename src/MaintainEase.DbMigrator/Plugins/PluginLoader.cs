using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces;

namespace MaintainEase.DbMigrator.Plugins
{
    /// <summary>
    /// Handles loading migration plugins
    /// </summary>
    public class PluginLoader
    {
        private readonly ILogger<PluginLoader> _logger;
        private readonly List<IMigrationPlugin> _plugins = new List<IMigrationPlugin>();
        private bool _isInitialized = false;

        public PluginLoader(ILogger<PluginLoader> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all loaded plugins
        /// </summary>
        public IReadOnlyCollection<IMigrationPlugin> Plugins => _plugins.AsReadOnly();

        /// <summary>
        /// Gets the default plugin
        /// </summary>
        public IMigrationPlugin DefaultPlugin => _plugins.FirstOrDefault(p => p.IsDefault) ?? _plugins.FirstOrDefault();

        /// <summary>
        /// Gets a plugin by provider type
        /// </summary>
        public IMigrationPlugin GetPlugin(string providerType)
        {
            EnsureInitialized();

            // Find plugin by provider type (case-insensitive)
            var plugin = _plugins.FirstOrDefault(p =>
                string.Equals(p.ProviderType, providerType, StringComparison.OrdinalIgnoreCase));

            if (plugin == null)
            {
                _logger.LogWarning("Plugin for provider '{ProviderType}' not found, using default plugin", providerType);
                return DefaultPlugin;
            }

            return plugin;
        }

        /// <summary>
        /// Initialize the plugin loader
        /// </summary>
        public void Initialize(string pluginsPath = null)
        {
            if (_isInitialized)
                return;

            _logger.LogInformation("Initializing plugin loader");

            // If no plugin path specified, use the default
            if (string.IsNullOrEmpty(pluginsPath))
            {
                pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            }

            _logger.LogInformation("Looking for plugins in {PluginsPath}", pluginsPath);

            // Create plugins directory if it doesn't exist
            if (!Directory.Exists(pluginsPath))
            {
                _logger.LogInformation("Creating plugins directory: {PluginsPath}", pluginsPath);
                Directory.CreateDirectory(pluginsPath);
            }

            // Load plugins from embedded resources
            LoadEmbeddedPlugins();

            // Look for plugin assemblies in the plugins directory
            var pluginFiles = Directory.GetFiles(pluginsPath, "*.dll");
            _logger.LogInformation("Found {Count} plugin files", pluginFiles.Length);

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    LoadPluginFromFile(pluginFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading plugin from {PluginFile}", pluginFile);
                }
            }

            // Set default plugin if none is set yet
            if (!_plugins.Any(p => p.IsDefault) && _plugins.Any())
            {
                var firstPlugin = _plugins.First();
                _logger.LogInformation("No default plugin set, using {PluginName} as default", firstPlugin.Name);
            }

            _isInitialized = true;
            _logger.LogInformation("Plugin loader initialized with {Count} plugins", _plugins.Count);
        }

        /// <summary>
        /// Load plugins directly embedded in the application
        /// </summary>
        private void LoadEmbeddedPlugins()
        {
            // We can also load plugins that are directly referenced by the application
            // This is useful for plugins that are always available

            _logger.LogInformation("Loading embedded plugins");

            var currentAssembly = Assembly.GetExecutingAssembly();
            LoadPluginsFromAssembly(currentAssembly);

            // Also check referenced assemblies with 'Plugin' in the name
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies()
                .Where(a => a.Name != null && a.Name.Contains("Plugin"))
                .ToList();

            foreach (var assemblyName in referencedAssemblies)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    LoadPluginsFromAssembly(assembly);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading plugins from assembly {AssemblyName}", assemblyName);
                }
            }
        }

        /// <summary>
        /// Load a plugin from a file
        /// </summary>
        private void LoadPluginFromFile(string pluginFile)
        {
            _logger.LogInformation("Loading plugins from {PluginFile}", pluginFile);

            try
            {
                // Load the assembly
                var pluginAssembly = Assembly.LoadFrom(pluginFile);
                LoadPluginsFromAssembly(pluginAssembly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assembly {PluginFile}", pluginFile);
            }
        }

        /// <summary>
        /// Load plugins from an assembly
        /// </summary>
        private void LoadPluginsFromAssembly(Assembly assembly)
        {
            try
            {
                // Find types that implement IMigrationPlugin
                var pluginTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IMigrationPlugin).IsAssignableFrom(t))
                    .ToList();

                _logger.LogInformation("Found {Count} plugin types in assembly {AssemblyName}",
                    pluginTypes.Count, assembly.GetName().Name);

                foreach (var pluginType in pluginTypes)
                {
                    try
                    {
                        // Create an instance of the plugin
                        var plugin = Activator.CreateInstance(pluginType) as IMigrationPlugin;

                        if (plugin != null)
                        {
                            _plugins.Add(plugin);
                            _logger.LogInformation("Loaded plugin: {PluginName} ({ProviderType})",
                                plugin.Name, plugin.ProviderType);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating instance of plugin type {PluginType}", pluginType.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading plugins from assembly {AssemblyName}", assembly.GetName().Name);
            }
        }

        /// <summary>
        /// Ensure the plugin loader is initialized
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }
    }
}
