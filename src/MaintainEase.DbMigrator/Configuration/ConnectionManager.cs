using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MaintainEase.DbMigrator.Configuration
{
    /// <summary>
    /// Manager for connection strings and database providers
    /// </summary>
    public class ConnectionManager
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConnectionManager> _logger;
        private readonly Dictionary<string, string> _providerConnectionStrings = new Dictionary<string, string>();
        private string _currentProvider = "SqlServer"; // Default to SQL Server

        public ConnectionManager(IConfiguration configuration, ILogger<ConnectionManager> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoadConnectionStrings();
        }

        /// <summary>
        /// Gets the current database provider
        /// </summary>
        public string CurrentProvider => _currentProvider;

        /// <summary>
        /// Get a connection string for the specified provider, or current provider if not specified
        /// </summary>
        public string GetConnectionString(string provider = null)
        {
            string targetProvider = provider ?? _currentProvider;

            if (_providerConnectionStrings.TryGetValue(targetProvider, out var connectionString))
            {
                return connectionString;
            }

            // If not found for specific provider, try default
            if (_providerConnectionStrings.TryGetValue("Default", out var defaultConnectionString))
            {
                return defaultConnectionString;
            }

            return null;
        }

        /// <summary>
        /// Set a connection string for a specific provider
        /// </summary>
        public void SetConnectionString(string provider, string connectionString)
        {
            _providerConnectionStrings[provider] = connectionString;
        }

        /// <summary>
        /// Switch the current provider
        /// </summary>
        public bool SwitchProvider(string provider)
        {
            if (string.IsNullOrEmpty(provider))
            {
                return false;
            }

            // Make provider name consistent
            var normalizedProvider = NormalizeProviderName(provider);

            // Check if we have a connection string for this provider
            if (!_providerConnectionStrings.ContainsKey(normalizedProvider))
            {
                _logger.LogWarning("No connection string found for provider: {Provider}", normalizedProvider);
                return false;
            }

            _currentProvider = normalizedProvider;
            _logger.LogInformation("Switched to provider: {Provider}", normalizedProvider);
            return true;
        }

        /// <summary>
        /// Get available database providers
        /// </summary>
        public List<string> GetAvailableProviders()
        {
            return _providerConnectionStrings.Keys.Where(k => k != "Default").ToList();
        }

        /// <summary>
        /// Load connection strings from configuration
        /// </summary>
        private void LoadConnectionStrings()
        {
            try
            {
                // Try to load the default provider from configuration
                var configProvider = _configuration["DbMigratorSettings:DatabaseProvider"];
                if (!string.IsNullOrEmpty(configProvider))
                {
                    _currentProvider = NormalizeProviderName(configProvider);
                    _logger.LogInformation("Using database provider from configuration: {Provider}", _currentProvider);
                }

                // Load connection strings from ConnectionStrings section
                var connectionStringsSection = _configuration.GetSection("ConnectionStrings");
                if (connectionStringsSection.Exists())
                {
                    foreach (var connectionString in connectionStringsSection.GetChildren())
                    {
                        var provider = DetermineProviderFromConnectionName(connectionString.Key);
                        _providerConnectionStrings[provider] = connectionString.Value;
                        _logger.LogDebug("Loaded connection string for {Provider} from ConnectionStrings", provider);
                    }
                }

                // Load connection strings from DbMigratorSettings
                var defaultConnectionString = _configuration["DbMigratorSettings:DefaultConnectionString"];
                if (!string.IsNullOrEmpty(defaultConnectionString))
                {
                    _providerConnectionStrings["Default"] = defaultConnectionString;

                    // Apply to current provider if not already set
                    if (!_providerConnectionStrings.ContainsKey(_currentProvider))
                    {
                        _providerConnectionStrings[_currentProvider] = defaultConnectionString;
                    }

                    _logger.LogDebug("Loaded default connection string from DbMigratorSettings");
                }

                // Load tenant-specific connection strings
                var tenantsSection = _configuration.GetSection("DbMigratorSettings:Tenants");
                if (tenantsSection.Exists())
                {
                    foreach (var tenant in tenantsSection.GetChildren())
                    {
                        var tenantConnectionString = tenant["ConnectionString"];
                        if (!string.IsNullOrEmpty(tenantConnectionString))
                        {
                            var tenantId = tenant["Identifier"] ?? "default";
                            _providerConnectionStrings[$"Tenant_{tenantId}"] = tenantConnectionString;
                            _logger.LogDebug("Loaded connection string for tenant: {Tenant}", tenantId);
                        }
                    }
                }

                // If no connection strings loaded, use a default local development connection string
                if (_providerConnectionStrings.Count == 0)
                {
                    _logger.LogWarning("No connection strings found in configuration. Using default local development connection string.");
                    _providerConnectionStrings["Default"] = "Server=localhost;Database=MaintainEase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true";
                    _providerConnectionStrings["SqlServer"] = "Server=localhost;Database=MaintainEase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading connection strings from configuration");

                // Provide fallback connection string
                _providerConnectionStrings["Default"] = "Server=localhost;Database=MaintainEase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true";
                _providerConnectionStrings["SqlServer"] = "Server=localhost;Database=MaintainEase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true";
            }
        }

        /// <summary>
        /// Determine the provider from a connection string name
        /// </summary>
        private string DetermineProviderFromConnectionName(string name)
        {
            if (name.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("MSSQL", StringComparison.OrdinalIgnoreCase))
            {
                return "SqlServer";
            }

            if (name.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                return "PostgreSQL";
            }

            if (name.Equals("DefaultConnection", StringComparison.OrdinalIgnoreCase))
            {
                return "Default";
            }

            return "Default";
        }

        /// <summary>
        /// Normalize provider name for consistency
        /// </summary>
        private string NormalizeProviderName(string provider)
        {
            if (string.IsNullOrEmpty(provider))
            {
                return "SqlServer"; // Default
            }

            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) ||
                provider.Equals("MSSQL", StringComparison.OrdinalIgnoreCase))
            {
                return "SqlServer";
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                return "PostgreSQL";
            }

            return provider;
        }

        /// <summary>
        /// Get a tenant-specific connection string
        /// </summary>
        public string GetTenantConnectionString(string tenant)
        {
            // First check for tenant-specific connection string
            var tenantKey = $"Tenant_{tenant}";
            if (_providerConnectionStrings.TryGetValue(tenantKey, out var connectionString))
            {
                return connectionString;
            }

            // Fall back to provider connection string
            return GetConnectionString();
        }
    }
}
