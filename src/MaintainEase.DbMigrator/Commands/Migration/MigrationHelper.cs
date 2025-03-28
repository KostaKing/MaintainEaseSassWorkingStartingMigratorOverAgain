using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.Contracts.Interfaces.Migrations;
using MaintainEase.DbMigrator.Plugins;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaintainEase.DbMigrator.Commands.Migration
{
    /// <summary>
    /// Helper class for migration operations
    /// </summary>
    public class MigrationHelper
    {
        private readonly ApplicationContext _appContext;
        private readonly PluginService _pluginService;
        private readonly ILogger<MigrationHelper> _logger;

        public MigrationHelper(
            ApplicationContext appContext,
            PluginService pluginService,
            ILogger<MigrationHelper> logger)
        {
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Check for pending migrations
        /// </summary>
        public async Task<MigrationStatus> CheckMigrationStatusAsync()
        {
            try
            {
                // Get the current connection string
                var connectionString = _appContext.GetConnectionString(_appContext.CurrentTenant);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Connection string not found for tenant {TenantId}", _appContext.CurrentTenant);
                    return new MigrationStatus
                    {
                        HasPendingMigrations = false,
                        PendingMigrationsCount = 0,
                        ErrorMessage = "Connection string not found"
                    };
                }

                // Create request
                var request = new MigrationRequest
                {
                    ConnectionConfig = new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        ProviderName = _appContext.CurrentProvider
                    },
                    TenantId = _appContext.CurrentTenant,
                    Environment = _appContext.CurrentEnvironment
                };

                // Get migration status from plugin
                var status = await _pluginService.GetStatusAsync(request);

                // Update application context with status information
                _appContext.HasPendingMigrations = status.HasPendingMigrations;
                _appContext.PendingMigrationsCount = status.PendingMigrationsCount;

                if (status.LastMigrationDate.HasValue)
                {
                    _appContext.LastMigrationDate = status.LastMigrationDate.Value;
                }

                if (!string.IsNullOrEmpty(status.LastMigrationName))
                {
                    _appContext.LastMigrationName = status.LastMigrationName;
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking migration status");
                return new MigrationStatus
                {
                    HasPendingMigrations = false,
                    PendingMigrationsCount = 0,
                    ErrorMessage = $"Error checking migration status: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Create a new migration
        /// </summary>
        public async Task<MigrationResult> CreateMigrationAsync(string migrationName, string outputDirectory = null)
        {
            try
            {
                // Generate name if not provided
                if (string.IsNullOrEmpty(migrationName))
                {
                    migrationName = GenerateDefaultMigrationName();
                }

                // Determine output directory
                outputDirectory ??= _appContext.MigrationsDirectory;

                // Get connection string
                var connectionString = _appContext.GetConnectionString(_appContext.CurrentTenant);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Connection string not found for tenant {TenantId}", _appContext.CurrentTenant);
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Connection string not found"
                    };
                }

                // Create request
                var request = new MigrationRequest
                {
                    ConnectionConfig = new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        ProviderName = _appContext.CurrentProvider
                    },
                    MigrationName = migrationName,
                    OutputDirectory = outputDirectory,
                    TenantId = _appContext.CurrentTenant,
                    Environment = _appContext.CurrentEnvironment
                };

                // Create migration
                var result = await _pluginService.CreateMigrationAsync(request);

                // If successful, check that the file was actually created
                if (result.Success && result.AppliedMigrations?.Count > 0)
                {
                    var migration = result.AppliedMigrations[0];
                    var scriptPath = migration.Script;

                    if (!string.IsNullOrEmpty(scriptPath) && !File.Exists(scriptPath))
                    {
                        // Try to create an empty migration file if it doesn't exist
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(scriptPath));
                            File.WriteAllText(scriptPath, $"-- Migration: {migrationName}\r\n-- Created: {DateTime.Now}\r\n\r\n-- Write your SQL here\r\n");
                            _logger.LogInformation("Created empty migration file at {ScriptPath}", scriptPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to create empty migration file at {ScriptPath}", scriptPath);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating migration {MigrationName}", migrationName);
                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = $"Error creating migration: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Run migrations
        /// </summary>
        public async Task<MigrationResult> RunMigrationsAsync(bool createBackup = true)
        {
            try
            {
                // Get connection string
                var connectionString = _appContext.GetConnectionString(_appContext.CurrentTenant);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Connection string not found for tenant {TenantId}", _appContext.CurrentTenant);
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Connection string not found"
                    };
                }

                // Create request
                var request = new MigrationRequest
                {
                    ConnectionConfig = new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        ProviderName = _appContext.CurrentProvider
                    },
                    CreateBackup = createBackup && _appContext.AutoBackupBeforeMigration,
                    TenantId = _appContext.CurrentTenant,
                    Environment = _appContext.CurrentEnvironment,
                    OutputDirectory = _appContext.MigrationsDirectory
                };

                // Run migrations
                var result = await _pluginService.MigrateAsync(request);

                // Update application context status
                if (result.Success)
                {
                    // Refresh migration status
                    await CheckMigrationStatusAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running migrations");
                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = $"Error running migrations: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Generate scripts for pending migrations
        /// </summary>
        public async Task<MigrationResult> GenerateScriptsAsync(string outputDirectory = null)
        {
            try
            {
                // Determine output directory
                outputDirectory ??= _appContext.ScriptsDirectory;

                // Get connection string
                var connectionString = _appContext.GetConnectionString(_appContext.CurrentTenant);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Connection string not found for tenant {TenantId}", _appContext.CurrentTenant);
                    return new MigrationResult
                    {
                        Success = false,
                        ErrorMessage = "Connection string not found"
                    };
                }

                // Create request
                var request = new MigrationRequest
                {
                    ConnectionConfig = new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        ProviderName = _appContext.CurrentProvider
                    },
                    OutputDirectory = outputDirectory,
                    TenantId = _appContext.CurrentTenant,
                    Environment = _appContext.CurrentEnvironment
                };

                // Generate scripts
                return await _pluginService.GenerateScriptsAsync(request);
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
        /// Test connection to database
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Get connection string
                var connectionString = _appContext.GetConnectionString(_appContext.CurrentTenant);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Connection string not found for tenant {TenantId}", _appContext.CurrentTenant);
                    return false;
                }

                // Create request
                var request = new MigrationRequest
                {
                    ConnectionConfig = new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        ProviderName = _appContext.CurrentProvider
                    },
                    TenantId = _appContext.CurrentTenant,
                    Environment = _appContext.CurrentEnvironment
                };

                // Test connection
                return await _pluginService.TestConnectionAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                return false;
            }
        }

        /// <summary>
        /// Generate a default migration name based on timestamp
        /// </summary>
        private string GenerateDefaultMigrationName()
        {
            return $"Migration_{DateTime.Now:yyyyMMdd_HHmmss}";
        }
    }
}
