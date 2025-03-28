using MaintainEase.DbMigrator.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaintainEase.DbMigrator.Plugins
{
    public static class Helpers
    {
        /// <summary>
        /// Ensure that required directories for plugins exist
        /// </summary>
        public static void EnsureRequiredDirectories(IServiceProvider serviceProvider = null)
        {
            try
            {
                // Create plugins directory if it doesn't exist
                var pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
                if (!Directory.Exists(pluginsDirectory))
                {
                    Directory.CreateDirectory(pluginsDirectory);
                    Console.WriteLine($"Created plugins directory: {pluginsDirectory}");

                    // If the directory was just created, copy any dummy plugins for testing
                    // This could be from embedded resources or assembly directory
                    var assemblyDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                    var dummyPluginsDir = Path.Combine(assemblyDir, "DummyPlugins");

                    if (Directory.Exists(dummyPluginsDir))
                    {
                        foreach (var file in Directory.GetFiles(dummyPluginsDir, "*.dll"))
                        {
                            var destFile = Path.Combine(pluginsDirectory, Path.GetFileName(file));
                            File.Copy(file, destFile, true);
                            Console.WriteLine($"Copied plugin: {Path.GetFileName(file)}");
                        }
                    }
                }

                // Create standard directories if serviceProvider is available
                if (serviceProvider != null)
                {
                    try
                    {
                        var appContext = serviceProvider.GetRequiredService<ApplicationContext>();
                        if (appContext != null)
                        {
                            // Initialize the application context which creates required directories
                            appContext.Initialize();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not initialize application context: {ex.Message}");
                        CreateBasicDirectories();
                    }
                }
                else
                {
                    // If no service provider available, create basic directories manually
                    Console.WriteLine("Warning: Service provider not available, creating basic directories manually");
                    CreateBasicDirectories();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating required directories: {ex.Message}");
            }
        }

        /// <summary>
        /// Create basic application directories without using ApplicationContext
        /// </summary>
        private static void CreateBasicDirectories()
        {
            var workingDir = Directory.GetCurrentDirectory();

            // Create standard directories
            var directories = new[]
            {
                Path.Combine(workingDir, "Scripts"),
                Path.Combine(workingDir, "Logs"),
                Path.Combine(workingDir, "Backups"),
                Path.Combine(workingDir, "Migrations")
            };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    Console.WriteLine($"Created directory: {dir}");
                }
            }
        }
    }
}
