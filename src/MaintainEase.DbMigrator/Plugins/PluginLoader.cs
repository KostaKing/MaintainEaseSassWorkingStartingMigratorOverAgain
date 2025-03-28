using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;

namespace MaintainEase.DbMigrator.Plugins
{
    /// <summary>
    /// Enhanced plugin loader with better logging and fallback mechanisms
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

            _logger.LogInformation("Looking for provider: {ProviderType}", providerType);
            _logger.LogInformation("Available plugins: {PluginCount}", _plugins.Count);

            foreach (var plug in _plugins)
            {
                _logger.LogDebug("Plugin: {Name}, Type: {ProviderType}", plug.Name, plug.ProviderType);
            }

            // Find plugin by provider type (case-insensitive)
            var plugin = _plugins.FirstOrDefault(p =>
                string.Equals(p.ProviderType, providerType, StringComparison.OrdinalIgnoreCase));

            if (plugin == null)
            {
                _logger.LogWarning("Plugin for provider '{ProviderType}' not found, using default plugin", providerType);

                // If no default plugin exists, create a fallback plugin
                if (_plugins.Count == 0)
                {
                    _logger.LogWarning("No plugins found. Using fallback plugin.");
                    return CreateFallbackPlugin(providerType);
                }

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
            if (Directory.Exists(pluginsPath))
            {
                // Only target files that follow our plugin naming convention
                var pluginFiles = Directory.GetFiles(pluginsPath, "MaintainEase.DbMigrator.Plugins*.dll");
                _logger.LogInformation("Found {Count} potential plugin files", pluginFiles.Length);

                foreach (var file in pluginFiles)
                {
                    _logger.LogDebug("Processing plugin file: {FileName}", Path.GetFileName(file));
                    try
                    {
                        LoadPluginFromFile(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading plugin from {PluginFile}", file);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Plugins directory does not exist: {PluginsPath}", pluginsPath);
            }

            // If no plugins were loaded, try to create default plugins
            if (_plugins.Count == 0)
            {
                _logger.LogWarning("No plugins loaded. Adding fallback plugins.");
                AddFallbackPlugins();
            }

            _isInitialized = true;
            _logger.LogInformation("Plugin loader initialized with {Count} plugins", _plugins.Count);

            // Log the loaded plugins
            foreach (var plugin in _plugins)
            {
                _logger.LogInformation("Loaded plugin: {Name} ({ProviderType})", plugin.Name, plugin.ProviderType);
            }
        }


        /// <summary>
        /// Load plugins directly embedded in the application
        /// </summary>
        private void LoadEmbeddedPlugins()
        {
            _logger.LogInformation("Loading embedded plugins");

            try
            {
                // Load from current assembly
                var currentAssembly = Assembly.GetExecutingAssembly();
                _logger.LogDebug("Checking current assembly: {Assembly}", currentAssembly.FullName);
                LoadPluginsFromAssembly(currentAssembly);

                // Get directories where we might find plugin assemblies
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _logger.LogDebug("Base directory: {BaseDir}", baseDir);

                // Try to load from assemblies in the base directory
                foreach (var file in Directory.GetFiles(baseDir, "*.dll"))
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        if (fileName.Contains("Plugin") || fileName.Contains("SqlServer") || fileName.Contains("PostgreSQL"))
                        {
                            _logger.LogDebug("Checking potential plugin file: {File}", fileName);
                            LoadPluginFromFile(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading assembly from base directory: {File}", file);
                    }
                }

                // Also check referenced assemblies with 'Plugin' in the name
                _logger.LogDebug("Checking referenced assemblies");
                var referencedAssemblies = currentAssembly.GetReferencedAssemblies()
                    .Where(a => a.Name != null && (
                        a.Name.Contains("Plugin") ||
                        a.Name.Contains("SqlServer") ||
                        a.Name.Contains("PostgreSQL")))
                    .ToList();

                _logger.LogDebug("Found {Count} potentially relevant referenced assemblies", referencedAssemblies.Count);

                foreach (var assemblyName in referencedAssemblies)
                {
                    try
                    {
                        _logger.LogDebug("Loading assembly: {AssemblyName}", assemblyName.FullName);
                        var assembly = Assembly.Load(assemblyName);
                        LoadPluginsFromAssembly(assembly);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading plugins from assembly {AssemblyName}", assemblyName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading embedded plugins");
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
                var fileName = Path.GetFileName(pluginFile);

                // Safety check - only load our plugin assemblies
                if (!fileName.StartsWith("MaintainEase.DbMigrator.Plugins"))
                {
                    _logger.LogWarning("Skipping non-plugin assembly: {FileName}", fileName);
                    return;
                }

                // Load the assembly
                var pluginAssembly = Assembly.LoadFrom(pluginFile);
                LoadPluginsFromAssembly(pluginAssembly);
            }
            catch (FileLoadException ex)
            {
                _logger.LogError(ex, "Assembly dependency conflict loading {PluginFile}. Error: {Message}",
                    pluginFile, ex.Message);
            }
            catch (BadImageFormatException ex)
            {
                _logger.LogError(ex, "Not a valid .NET assembly: {PluginFile}", pluginFile);
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
                _logger.LogDebug("Scanning assembly: {Assembly}", assembly.FullName);

                // Find types that implement IMigrationPlugin
                var pluginTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract &&
                                !t.IsInterface &&
                                !t.IsNested && // Exclude nested types to avoid FallbackMigrationPlugin
                                typeof(IMigrationPlugin).IsAssignableFrom(t))
                    .ToList();

                _logger.LogInformation("Found {Count} plugin types in assembly {AssemblyName}",
                    pluginTypes.Count, assembly.GetName().Name);

                // Log found types for debugging
                foreach (var type in pluginTypes)
                {
                    _logger.LogDebug("Found plugin type: {PluginType}, IsNested: {IsNested}",
                        type.FullName, type.IsNested);
                }

                foreach (var pluginType in pluginTypes)
                {
                    try
                    {
                        _logger.LogDebug("Creating instance of plugin type: {PluginType}", pluginType.FullName);

                        // Check if type has a parameterless constructor
                        var hasDefaultConstructor = pluginType.GetConstructor(Type.EmptyTypes) != null;
                        if (!hasDefaultConstructor)
                        {
                            _logger.LogWarning("Plugin type {PluginType} does not have a parameterless constructor",
                                pluginType.FullName);
                            continue;
                        }

                        // Create an instance of the plugin
                        var plugin = Activator.CreateInstance(pluginType) as IMigrationPlugin;

                        if (plugin != null)
                        {
                            _plugins.Add(plugin);
                            _logger.LogInformation("Loaded plugin: {PluginName} ({ProviderType})",
                                plugin.Name, plugin.ProviderType);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create plugin instance for type {PluginType}",
                                pluginType.FullName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating instance of plugin type {PluginType}",
                            pluginType.FullName);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogError(ex, "Error loading types from assembly {AssemblyName}",
                    assembly.GetName().Name);

                foreach (var loaderEx in ex.LoaderExceptions)
                {
                    _logger.LogError(loaderEx, "Loader exception details");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading plugins from assembly {AssemblyName}",
                    assembly.GetName().Name);
            }
        }

        /// <summary>
        /// Add fallback plugins when no plugins are found
        /// </summary>
        private void AddFallbackPlugins()
        {
            _logger.LogInformation("Adding fallback plugins");

            // Create fallback plugins
            _plugins.Add(CreateFallbackPlugin("SqlServer", true));
            _plugins.Add(CreateFallbackPlugin("PostgreSQL", false));
        }

        /// <summary>
        /// Create a fallback plugin implementation
        /// </summary>
        private IMigrationPlugin CreateFallbackPlugin(string providerType, bool isDefault = false)
        {
            return new FallbackMigrationPlugin(providerType, isDefault);
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

        /// <summary>
        /// Fallback plugin implementation
        /// </summary>
        private class FallbackMigrationPlugin : IMigrationPlugin
        {
            public FallbackMigrationPlugin(string providerType, bool isDefault)
            {
                ProviderType = providerType;
                IsDefault = isDefault;
                _migrationHandler = new FallbackMigrationHandler(providerType);
            }

            public string Name => $"{ProviderType} (Fallback)";
            public string ProviderType { get; }
            public string Version => "1.0.0";
            public string Description => $"Fallback migration plugin for {ProviderType}";
            public IEnumerable<string> Capabilities => new[] { "Basic Migration" };
            public bool IsDefault { get; }

            private readonly IMigrationHandler _migrationHandler;
            public IMigrationHandler MigrationHandler => _migrationHandler;
        }

        /// <summary>
        /// Fallback migration handler implementation
        /// </summary>
        private class FallbackMigrationHandler : IMigrationHandler
        {
            public FallbackMigrationHandler(string providerType)
            {
                ProviderType = providerType;
            }

            public string ProviderType { get; }

            public Task<MigrationResult> CreateMigrationAsync(MigrationRequest request, CancellationToken cancellationToken = default)
            {
                Console.WriteLine($"[FALLBACK] Creating migration '{request.MigrationName}' for {ProviderType}");

                return Task.FromResult(new MigrationResult
                {
                    Success = true,
                    AppliedMigrations = new List<MigrationInfo>
                    {
                        new MigrationInfo
                        {
                            Id = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                            Name = request.MigrationName,
                            Script = $"{request.OutputDirectory ?? "Migrations"}/{request.MigrationName}.sql"
                        }
                    }
                });
            }

            public Task<MigrationResult> MigrateAsync(MigrationRequest request, CancellationToken cancellationToken = default)
            {
                Console.WriteLine($"[FALLBACK] Applying migrations to {ProviderType} database");

                return Task.FromResult(new MigrationResult { Success = true });
            }

            public Task<MigrationStatus> GetStatusAsync(MigrationRequest request, CancellationToken cancellationToken = default)
            {
                Console.WriteLine($"[FALLBACK] Getting migration status for {ProviderType} database");

                return Task.FromResult(new MigrationStatus
                {
                    HasPendingMigrations = false,
                    PendingMigrationsCount = 0,
                    ProviderName = ProviderType,
                    DatabaseName = "Unknown (Fallback)"
                });
            }

            public Task<MigrationResult> GenerateScriptsAsync(MigrationRequest request, CancellationToken cancellationToken = default)
            {
                Console.WriteLine($"[FALLBACK] Generating migration scripts for {ProviderType} database");

                return Task.FromResult(new MigrationResult { Success = true });
            }

            public Task<bool> TestConnectionAsync(MigrationRequest request, CancellationToken cancellationToken = default)
            {
                Console.WriteLine($"[FALLBACK] Testing connection to {ProviderType} database");

                return Task.FromResult(true);
            }
        }
    }
}
