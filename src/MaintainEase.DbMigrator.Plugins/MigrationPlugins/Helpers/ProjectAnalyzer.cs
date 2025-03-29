using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MaintainEase.DbMigrator.Plugins.MigrationPlugins.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static MaintainEase.DbMigrator.Plugins.MigrationPlugins.Handlers.SqlServerMigrationHandler;

namespace MaintainEase.DbMigrator.Plugins.MigrationPlugins.Helpers
{
    /// <summary>
    /// Analyzer for .NET projects and solutions
    /// </summary>
    public class ProjectAnalyzer
    {
        private readonly ILogger _logger;

        public ProjectAnalyzer(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Find the dotnet executable
        /// </summary>
        public string FindDotnetExecutable()
        {
            // Try to find dotnet in PATH
            foreach (var path in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, "dotnet.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // Default locations
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var possiblePaths = new[]
            {
                Path.Combine(programFiles, "dotnet", "dotnet.exe"),
                Path.Combine(programFilesX86, "dotnet", "dotnet.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Assume dotnet is in PATH
            return "dotnet";
        }

        /// <summary>
        /// Find the solution directory
        /// </summary>
        public string FindSolutionDirectory(string startPath)
        {
            var directory = new DirectoryInfo(startPath);

            while (directory != null)
            {
                if (directory.GetFiles("*.sln").Any())
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }

        /// <summary>
        /// Analyze the Infrastructure project to find DbContext classes
        /// </summary>
        public async Task<List<DbContextInfo>> AnalyzeDbContextsInProject(string projectPath)
        {
            var result = new List<DbContextInfo>();

            try
            {
                // Look for existing compiled DLLs first
                var configurations = new[] { "Debug", "Release" };
                var frameworks = new[] { "net9.0", "net8.0", "net7.0", "net6.0" };
                string dllPath = null;

                foreach (var config in configurations)
                {
                    foreach (var framework in frameworks)
                    {
                        var path = Path.Combine(projectPath, "bin", config, framework, "MaintainEase.Infrastructure.dll");
                        if (File.Exists(path))
                        {
                            dllPath = path;
                            _logger?.LogInformation("Found Infrastructure DLL at {Path}", path);
                            break;
                        }
                    }
                    if (dllPath != null) break;
                }

                if (dllPath == null)
                {
                    // If DLL not found, we'll need to build the project
                    await BuildProjectAsync(projectPath);

                    // Try to find the DLL again after building
                    foreach (var config in configurations)
                    {
                        foreach (var framework in frameworks)
                        {
                            var path = Path.Combine(projectPath, "bin", config, framework, "MaintainEase.Infrastructure.dll");
                            if (File.Exists(path))
                            {
                                dllPath = path;
                                _logger?.LogInformation("Found Infrastructure DLL after building at {Path}", path);
                                break;
                            }
                        }
                        if (dllPath != null) break;
                    }
                }

                if (dllPath == null)
                {
                    _logger?.LogWarning("Could not find compiled Infrastructure DLL after building.");

                    // Fallback: analyze source code directly to find DbContext classes
                    return await AnalyzeSourceCodeForDbContexts(projectPath);
                }

                // Use reflection to find DbContext types in the DLL
                try
                {
                    var assembly = Assembly.LoadFrom(dllPath);

                    // Find types that derive from DbContext
                    var dbContextBaseType = typeof(DbContext);
                    var types = assembly.GetTypes()
                        .Where(t => t != dbContextBaseType && // Not the base DbContext type
                                    dbContextBaseType.IsAssignableFrom(t) && // Is or inherits from DbContext
                                    !t.IsAbstract && // Not abstract
                                    !t.IsInterface) // Not an interface
                        .ToList();

                    foreach (var type in types)
                    {
                        result.Add(new DbContextInfo
                        {
                            Name = type.Name,
                            FullName = type.FullName,
                            Namespace = type.Namespace,
                            Assembly = assembly.GetName().Name
                        });

                        _logger?.LogInformation("Found DbContext: {Name} ({FullName})", type.Name, type.FullName);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error loading assembly for reflection: {DllPath}", dllPath);

                    // Fallback: analyze source code
                    return await AnalyzeSourceCodeForDbContexts(projectPath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error analyzing Infrastructure project for DbContext classes");
            }

            return result;
        }

        /// <summary>
        /// Build the Infrastructure project
        /// </summary>
        public async Task<bool> BuildProjectAsync(string projectPath)
        {
            try
            {
                _logger?.LogInformation("Building Infrastructure project at {Path}", projectPath);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{projectPath}\" -c Debug",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger?.LogError("Failed to build Infrastructure project: {Error}", error);
                    return false;
                }

                _logger?.LogInformation("Successfully built Infrastructure project");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error building Infrastructure project");
                return false;
            }
        }

        /// <summary>
        /// Fallback: analyze source code directly to find DbContext classes
        /// </summary>
        public async Task<List<DbContextInfo>> AnalyzeSourceCodeForDbContexts(string projectPath)
        {
            var result = new List<DbContextInfo>();

            try
            {
                _logger?.LogInformation("Analyzing source code to find DbContext classes");

                // Look for .cs files in the project
                var sourceFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

                foreach (var file in sourceFiles)
                {
                    try
                    {
                        string content = await File.ReadAllTextAsync(file);

                        // Look for class declarations that inherit from DbContext
                        if (content.Contains("DbContext") &&
                            (content.Contains(" : DbContext") || content.Contains(" : Microsoft.EntityFrameworkCore.DbContext")))
                        {
                            // Parse the file to extract namespace and class name
                            string namespaceName = ExtractNamespace(content);
                            string className = ExtractClassName(content, "DbContext");

                            if (!string.IsNullOrEmpty(className))
                            {
                                result.Add(new DbContextInfo
                                {
                                    Name = className,
                                    FullName = string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}",
                                    Namespace = namespaceName,
                                    Assembly = "MaintainEase.Infrastructure",
                                    SourceFile = file
                                });

                                _logger?.LogInformation("Found DbContext in source: {Name} ({FullName})",
                                    className, string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error analyzing source file: {File}", file);
                        // Continue to next file
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error analyzing source code");
            }

            return result;
        }

        /// <summary>
        /// Extract namespace from source code
        /// </summary>
        public string ExtractNamespace(string content)
        {
            var match = Regex.Match(content, @"namespace\s+([^\s;{]+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        /// <summary>
        /// Extract class name from source code
        /// </summary>
        public string ExtractClassName(string content, string baseClass)
        {
            // Look for a class that inherits from the specified base class
            var match = Regex.Match(content, @"class\s+([^\s:;{]+)(?:\s*:\s*(?:[^{;]*\s)?" + baseClass + @")");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        /// <summary>
        /// Helper method to output project diagnostics
        /// </summary>
        public async Task<string> GenerateProjectDiagnosticsAsync(string projectPath)
        {
            var sb = new StringBuilder();

            // Check if project file exists
            if (!File.Exists(Path.Combine(projectPath, "MaintainEase.Infrastructure.csproj")))
            {
                sb.AppendLine("ERROR: Infrastructure project file not found!");

                // List files in the directory to help diagnose
                if (Directory.Exists(projectPath))
                {
                    sb.AppendLine("Files in directory:");
                    foreach (var file in Directory.GetFiles(projectPath, "*.csproj"))
                    {
                        sb.AppendLine($"  - {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    sb.AppendLine("Directory does not exist!");
                }
            }
            else
            {
                sb.AppendLine("Found Infrastructure project file.");

                // Check for DbContext classes
                var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
                var dbContextFiles = csFiles.Where(f => File.ReadAllText(f).Contains(" DbContext") ||
                                                       File.ReadAllText(f).Contains(":DbContext") ||
                                                       File.ReadAllText(f).Contains(": DbContext")).ToList();

                if (dbContextFiles.Any())
                {
                    sb.AppendLine("Found DbContext classes:");
                    foreach (var file in dbContextFiles)
                    {
                        sb.AppendLine($"  - {Path.GetRelativePath(projectPath, file)}");
                    }
                }
                else
                {
                    sb.AppendLine("WARNING: No DbContext classes found!");
                }
            }

            return sb.ToString();
        }
    }
}
