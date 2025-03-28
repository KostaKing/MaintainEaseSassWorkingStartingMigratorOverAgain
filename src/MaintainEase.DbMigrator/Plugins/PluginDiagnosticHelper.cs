using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MaintainEase.DbMigrator.Plugins
{
    /// <summary>
    /// Helper class for plugin system diagnostics
    /// </summary>
    public static class PluginDiagnosticHelper
    {
        /// <summary>
        /// Logs detailed diagnostic information about the plugin system
        /// </summary>
        public static void LogDiagnosticInfo(ILogger logger)
        {
            try
            {
                // Log basic environment info
                logger.LogInformation("=== Plugin System Diagnostic Info ===");
                logger.LogInformation("Current Directory: {Path}", Directory.GetCurrentDirectory());
                logger.LogInformation("Base Directory: {Path}", AppDomain.CurrentDomain.BaseDirectory);

                // Check plugins directory
                var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                if (Directory.Exists(pluginsPath))
                {
                    logger.LogInformation("Plugins directory exists at: {Path}", pluginsPath);

                    // List all DLLs but don't try to load them all
                    var allDllFiles = Directory.GetFiles(pluginsPath, "*.dll");
                    logger.LogInformation("Total DLL files in plugins directory: {Count}", allDllFiles.Length);
                    foreach (var file in allDllFiles)
                    {
                        logger.LogDebug("Found DLL: {FileName}", Path.GetFileName(file));
                    }

                    // Only examine potential plugin DLLs (don't load everything)
                    var pluginFiles = Directory.GetFiles(pluginsPath, "MaintainEase.DbMigrator.Plugins*.dll");
                    logger.LogInformation("Potential plugin DLLs: {Count}", pluginFiles.Length);

                    foreach (var file in pluginFiles)
                    {
                        logger.LogInformation("Examining plugin file: {FileName}", Path.GetFileName(file));
                        try
                        {
                            var assembly = Assembly.LoadFrom(file);
                            logger.LogInformation("Successfully loaded assembly: {AssemblyName}", assembly.FullName);

                            // Check for our plugin interfaces
                            var migrationPluginTypes = assembly.GetTypes()
                                .Where(t => typeof(Contracts.Interfaces.Migrations.IMigrationPlugin).IsAssignableFrom(t)
                                       && !t.IsAbstract && !t.IsInterface)
                                .ToList();

                            logger.LogInformation("Found {Count} IMigrationPlugin implementations in {AssemblyName}",
                                migrationPluginTypes.Count, assembly.GetName().Name);

                            foreach (var type in migrationPluginTypes)
                            {
                                logger.LogInformation("  Plugin Type: {TypeName}", type.FullName);
                            }

                            // Also check for handlers
                            var handlerTypes = assembly.GetTypes()
                                .Where(t => typeof(Contracts.Interfaces.Migrations.IMigrationHandler).IsAssignableFrom(t)
                                       && !t.IsAbstract && !t.IsInterface)
                                .ToList();

                            logger.LogInformation("Found {Count} IMigrationHandler implementations in {AssemblyName}",
                                handlerTypes.Count, assembly.GetName().Name);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error examining plugin file: {FileName}", Path.GetFileName(file));
                        }
                    }
                }
                else
                {
                    logger.LogWarning("Plugins directory not found at: {Path}", pluginsPath);
                }

                // Check current assembly for plugins as well
                var currentAssembly = Assembly.GetExecutingAssembly();
                logger.LogInformation("Checking executing assembly: {AssemblyName}", currentAssembly.FullName);

                var currentAssemblyPlugins = currentAssembly.GetTypes()
                    .Where(t => typeof(Contracts.Interfaces.Migrations.IMigrationPlugin).IsAssignableFrom(t)
                           && !t.IsAbstract && !t.IsInterface)
                    .ToList();

                logger.LogInformation("Found {Count} IMigrationPlugin implementations in executing assembly",
                    currentAssemblyPlugins.Count);

                logger.LogInformation("=== End Diagnostic Info ===");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during plugin system diagnostics");
            }
        }
    }
}
