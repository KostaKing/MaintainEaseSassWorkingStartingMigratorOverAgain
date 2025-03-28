using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaintainEase.DbMigrator.Configuration
{
    public class ApplicationContext
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApplicationContext> _logger;

        // General settings
        public Dictionary<string, object> Settings { get; } = new Dictionary<string, object>();
        public string CurrentEnvironment { get; set; } = "Development";
        public string ProjectName { get; set; } = "MaintainEase";
        public string Version { get; set; } = "1.0.0";

        // Database related properties
        public string CurrentProvider { get; set; } = "PostgreSQL";
        public string DefaultConnectionString { get; set; }
        public List<string> AvailableTenants { get; set; } = new List<string>();
        public string CurrentTenant { get; set; } = "Default";

        // Migration related properties
        public bool HasPendingMigrations { get; set; }
        public int PendingMigrationsCount { get; set; }
        public DateTime? LastMigrationDate { get; set; }
        public string LastMigrationName { get; set; }

        // File paths
        public string WorkingDirectory { get; set; }
        public string ScriptsDirectory { get; set; }
        public string LogsDirectory { get; set; }
        public string BackupsDirectory { get; set; }
        public string MigrationsDirectory { get; set; }

        // Runtime state
        public bool IsInitialized { get; private set; }
        public bool IsConsoleMode { get; set; } = true;
        public bool IsDebugMode { get; set; }
        public bool IsBatchMode { get; set; }
        public bool IsInteractive => !IsBatchMode;

        // User preferences
        public string PreferredTheme { get; set; } = "Default";
        public bool ConfirmDangerousOperations { get; set; } = true;
        public bool AutoBackupBeforeMigration { get; set; } = true;
        public int CommandTimeout { get; set; } = 30;

        // Connection settings
        public Dictionary<string, string> ConnectionStrings { get; } = new Dictionary<string, string>();
        public int ConnectionRetryCount { get; set; } = 3;
        public TimeSpan ConnectionRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

        public ApplicationContext(IConfiguration configuration = null, ILogger<ApplicationContext> logger = null)
        {
            _configuration = configuration;
            _logger = logger;

            // Initialize with default values
            InitializeDefaults();

            // Load from configuration if available
            if (_configuration != null)
            {
                LoadFromConfiguration();
            }
        }

        private void InitializeDefaults()
        {
            // Set working directory to current directory if not specified
            WorkingDirectory = Directory.GetCurrentDirectory();

            // Set default paths
            ScriptsDirectory = Path.Combine(WorkingDirectory, "Scripts");
            LogsDirectory = Path.Combine(WorkingDirectory, "Logs");
            BackupsDirectory = Path.Combine(WorkingDirectory, "Backups");
            MigrationsDirectory = Path.Combine(WorkingDirectory, "Migrations");

            // Get environment from environment variable if available
            var envVar = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrEmpty(envVar))
            {
                CurrentEnvironment = envVar;
            }

            // Debug mode based on environment
            IsDebugMode = CurrentEnvironment == "Development";
        }

        private void LoadFromConfiguration(IConfiguration configToUse = null)
        {
            var config = configToUse ?? _configuration;
            
            try
            {
                // Load general settings
                ProjectName = config["Application:Name"] ?? ProjectName;
                Version = config["Application:Version"] ?? Version;

                // Load database settings
                CurrentProvider = _configuration["Database:Provider"] ?? CurrentProvider;
                DefaultConnectionString = _configuration.GetConnectionString("DefaultConnection") ?? DefaultConnectionString;

                // Load connection strings
                var connectionStringsSection = _configuration.GetSection("ConnectionStrings");
                if (connectionStringsSection.Exists())
                {
                    foreach (var connectionString in connectionStringsSection.GetChildren())
                    {
                        ConnectionStrings[connectionString.Key] = connectionString.Value;
                    }
                }

                // Load tenant list if available
                var tenantsSection = _configuration.GetSection("Tenants");
                if (tenantsSection.Exists())
                {
                    foreach (var tenant in tenantsSection.GetChildren())
                    {
                        AvailableTenants.Add(tenant.Key);
                    }
                }

                // Load paths if available
                WorkingDirectory = _configuration["Paths:WorkingDirectory"] ?? WorkingDirectory;
                ScriptsDirectory = _configuration["Paths:ScriptsDirectory"] ?? ScriptsDirectory;
                LogsDirectory = _configuration["Paths:LogsDirectory"] ?? LogsDirectory;
                BackupsDirectory = _configuration["Paths:BackupsDirectory"] ?? BackupsDirectory;
                MigrationsDirectory = _configuration["Paths:MigrationsDirectory"] ?? MigrationsDirectory;

                // Load user preferences
                PreferredTheme = _configuration["UserPreferences:Theme"] ?? PreferredTheme;

                if (bool.TryParse(_configuration["UserPreferences:ConfirmDangerousOperations"], out var confirmDangerous))
                {
                    ConfirmDangerousOperations = confirmDangerous;
                }

                if (bool.TryParse(_configuration["UserPreferences:AutoBackupBeforeMigration"], out var autoBackup))
                {
                    AutoBackupBeforeMigration = autoBackup;
                }

                if (int.TryParse(_configuration["UserPreferences:CommandTimeout"], out var commandTimeout))
                {
                    CommandTimeout = commandTimeout;
                }

                // Load connection retry settings
                if (int.TryParse(_configuration["Connection:RetryCount"], out var retryCount))
                {
                    ConnectionRetryCount = retryCount;
                }

                if (int.TryParse(_configuration["Connection:RetryDelaySeconds"], out var retryDelay))
                {
                    ConnectionRetryDelay = TimeSpan.FromSeconds(retryDelay);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error loading configuration into application context");
            }
        }

        /// <summary>
        /// Initialize the application context
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
                return;

            // Create required directories if they don't exist
            EnsureDirectoryExists(ScriptsDirectory);
            EnsureDirectoryExists(LogsDirectory);
            EnsureDirectoryExists(BackupsDirectory);
            EnsureDirectoryExists(MigrationsDirectory);

            IsInitialized = true;
            _logger?.LogInformation("Application context initialized successfully");
        }

        /// <summary>
        /// Set a setting value
        /// </summary>
        public void SetSetting(string key, object value)
        {
            Settings[key] = value;
        }

        /// <summary>
        /// Get a setting value
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            if (Settings.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Add or update connection string
        /// </summary>
        public void SetConnectionString(string name, string connectionString)
        {
            ConnectionStrings[name] = connectionString;
        }

        /// <summary>
        /// Get connection string by name
        /// </summary>
        public string GetConnectionString(string name)
        {
            return ConnectionStrings.TryGetValue(name, out var connectionString)
                ? connectionString
                : null;
        }

        /// <summary>
        /// Add a tenant to the available tenants list
        /// </summary>
        public void AddTenant(string tenant)
        {
            if (!AvailableTenants.Contains(tenant))
            {
                AvailableTenants.Add(tenant);
            }
        }

        /// <summary>
        /// Check if a tenant exists
        /// </summary>
        public bool TenantExists(string tenant)
        {
            return AvailableTenants.Contains(tenant);
        }

        /// <summary>
        /// Switch current tenant
        /// </summary>
        public async Task<bool> SwitchTenant(string tenant)
        {
            if (!TenantExists(tenant))
            {
                return false;
            }

            CurrentTenant = tenant;

            try
            {
                // Perform any tenant-specific initialization here
                // For example, check for pending migrations

                // Example: 
                // HasPendingMigrations = await tenantService.CheckForPendingMigrationsAsync(tenant);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error switching to tenant {Tenant}", tenant);
                return false;
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    _logger?.LogInformation("Created directory: {Path}", path);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create directory: {Path}", path);
                }
            }
        }

        /// <summary>
        /// Save current state to configuration file
        /// </summary>
        public bool SaveSettings(string filePath = null)
        {
            filePath ??= Path.Combine(WorkingDirectory, "dbmigrator.settings.json");

            try
            {
                // Create settings object to serialize
                var settings = new
                {
                    Application = new
                    {
                        Name = ProjectName,
                        Version = Version,
                        Environment = CurrentEnvironment
                    },
                    Database = new
                    {
                        Provider = CurrentProvider,
                        CurrentTenant = CurrentTenant
                    },
                    Paths = new
                    {
                        WorkingDirectory,
                        ScriptsDirectory,
                        LogsDirectory,
                        BackupsDirectory,
                        MigrationsDirectory
                    },
                    UserPreferences = new
                    {
                        Theme = PreferredTheme,
                        ConfirmDangerousOperations,
                        AutoBackupBeforeMigration,
                        CommandTimeout
                    },
                    Connection = new
                    {
                        RetryCount = ConnectionRetryCount,
                        RetryDelaySeconds = (int)ConnectionRetryDelay.TotalSeconds
                    }
                };

                // Serialize to JSON
                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Write to file
                File.WriteAllText(filePath, json);
                _logger?.LogInformation("Saved settings to {FilePath}", filePath);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save settings to {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Load settings from a configuration file
        /// </summary>
        public bool LoadSettings(string filePath = null)
        {
            filePath ??= Path.Combine(WorkingDirectory, "dbmigrator.settings.json");

            if (!File.Exists(filePath))
            {
                _logger?.LogWarning("Settings file not found: {FilePath}", filePath);
                return false;
            }

            try
            {
                // Read JSON from file
                var json = File.ReadAllText(filePath);

                // Create a temporary configuration from the JSON
                var configBuilder = new ConfigurationBuilder()
                    .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

                var tempConfig = configBuilder.Build();
                LoadFromConfiguration(tempConfig);

                // Load settings from this configuration


                _logger?.LogInformation("Loaded settings from {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load settings from {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Check if the application is running in batch mode
        /// </summary>
        public void DetectBatchMode()
        {
            // Check if the application is running in a CI/CD pipeline or automated environment
            IsBatchMode = Environment.GetEnvironmentVariable("CI") != null ||
                        Environment.GetEnvironmentVariable("BATCH_MODE") != null ||
                        !Console.IsInputRedirected;
        }

        /// <summary>
        /// Reset the application context to default values
        /// </summary>
        public void Reset()
        {
            Settings.Clear();
            ConnectionStrings.Clear();
            AvailableTenants.Clear();

            InitializeDefaults();

            IsInitialized = false;
        }
    }
}
