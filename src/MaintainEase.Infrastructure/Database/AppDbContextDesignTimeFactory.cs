using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MaintainEase.Infrastructure.Data.Context;
using MaintainEase.Infrastructure.Data.Interceptors;
using MaintainEase.Infrastructure.MultiTenancy;
using MaintainEase.Core.Domain.Events;
using MaintainEase.Core.Domain.ValueObjects;
using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Infrastructure.Database
{
    public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            Console.WriteLine("============== AppDbContextDesignTimeFactory ==============");
            Console.WriteLine("Creating DbContext for migrations with args:");
            foreach (var arg in args)
            {
                Console.WriteLine($"  {arg}");
            }

            // Log current environment
            Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set"}");

            // Parse command-line arguments
            string provider = "SqlServer";
            string connectionString = null;
            string outputDir = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--provider" && i + 1 < args.Length)
                {
                    provider = args[i + 1];
                    Console.WriteLine($"Found provider argument: {provider}");
                }
                else if (args[i] == "--connection" && i + 1 < args.Length)
                {
                    connectionString = args[i + 1];
                    Console.WriteLine($"Found connection string argument: {connectionString.Substring(0, Math.Min(20, connectionString.Length))}...");
                }
                else if (args[i] == "--output-dir" && i + 1 < args.Length)
                {
                    outputDir = args[i + 1];
                    Console.WriteLine($"Found output directory argument: {outputDir}");
                }
            }

            // Load configuration if no explicit connection string
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("No connection string provided in args, looking in configuration files...");
                try
                {
                    var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false)
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                        .Build();

                    Console.WriteLine("Configuration loaded, checking for connection strings...");

                    // Try different potential connection string locations
                    var connectionStringSources = new Dictionary<string, string>
                    {
                        { "ConnectionStrings:DefaultConnection", configuration.GetConnectionString("DefaultConnection") },
                        { "DbMigratorSettings:DefaultConnectionString", configuration["DbMigratorSettings:DefaultConnectionString"] },
                        { "Database:ConnectionString", configuration["Database:ConnectionString"] }
                    };

                    foreach (var source in connectionStringSources)
                    {
                        Console.WriteLine($"Checking {source.Key}: {(string.IsNullOrEmpty(source.Value) ? "Not found" : "Found")}");
                        if (!string.IsNullOrEmpty(source.Value))
                        {
                            connectionString = source.Value;
                            Console.WriteLine($"Using connection string from {source.Key}");
                            break;
                        }
                    }

                    // Try to get provider from config if not specified in args
                    if (provider == "SqlServer")
                    {
                        var providerFromConfig = configuration["DbMigratorSettings:DatabaseProvider"] ??
                                                 configuration["Database:Provider"];
                        if (!string.IsNullOrEmpty(providerFromConfig))
                        {
                            provider = providerFromConfig;
                            Console.WriteLine($"Using provider from configuration: {provider}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading configuration: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Could not find a connection string in configuration!");
                    throw new InvalidOperationException("Could not find a connection string in configuration");
                }
            }

            // Configure options based on provider
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            Console.WriteLine($"Configuring DbContext with provider: {provider}");

            // Configure DbContext based on provider
            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Using Npgsql provider");
                optionsBuilder.UseNpgsql(connectionString, opt =>
                {
                    opt.MigrationsHistoryTable("__EFMigrationsHistory");

                    // Handle output directory if specified
                    if (!string.IsNullOrEmpty(outputDir))
                    {
                        Console.WriteLine($"Setting migrations assembly path: {outputDir}");
                        opt.MigrationsAssembly(Path.GetFileName(Directory.GetCurrentDirectory()));
                    }

                    Console.WriteLine("Configured PostgreSQL options");
                });
            }
            else
            {
                Console.WriteLine("Using SQL Server provider");
                optionsBuilder.UseSqlServer(connectionString, opt =>
                {
                    opt.MigrationsHistoryTable("__EFMigrationsHistory");

                    // Handle output directory if specified
                    if (!string.IsNullOrEmpty(outputDir))
                    {
                        Console.WriteLine($"Setting migrations assembly path: {outputDir}");
                        opt.MigrationsAssembly(Path.GetFileName(Directory.GetCurrentDirectory()));
                    }

                    Console.WriteLine("Configured SQL Server options");
                });
            }

            // If output directory was specified, ensure it exists
            if (!string.IsNullOrEmpty(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                    Console.WriteLine($"Ensured output directory exists: {outputDir}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not create output directory: {ex.Message}");
                }
            }

            // Create and return the context with minimal dependencies
            Console.WriteLine("Creating MigrationDesignTimeContext");
            var context = new MigrationDesignTimeContext(optionsBuilder.Options);
            Console.WriteLine("DbContext created successfully");

            return context;
        }

        /// <summary>
        /// Minimal design-time context with dummy services
        /// </summary>
        /// <summary>
        /// Minimal design-time context with dummy services
        /// </summary>
        public class MigrationDesignTimeContext : AppDbContext
        {
            // Dummy implementations for required services
            private class DummyTenantProvider : ITenantProvider
            {
                public Guid GetCurrentTenantId() => Guid.Empty;
                public string GetCurrentTenantName() => "Migration";
                public string GetCurrentTenantConnectionString() => string.Empty;
            }

            private class DummyAuditInterceptor : IAuditInterceptor
            {
                public void ApplyAuditInformation(DbContext context) { }
            }

            private class DummyDomainEventInterceptor : IDomainEventInterceptor
            {
                public List<IDomainEvent> CaptureEvents(DbContext context) => new List<IDomainEvent>();
                public Task DispatchEventsAsync(List<IDomainEvent> events, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public void DispatchEvents(List<IDomainEvent> events) { }
            }

            public MigrationDesignTimeContext(DbContextOptions<AppDbContext> options)
                : base(options,
                        new DummyTenantProvider(),
                        new DummyAuditInterceptor(),
                        new DummyDomainEventInterceptor())
            {
                Console.WriteLine("Migration design-time context initialized");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                Console.WriteLine("Configuring model in MigrationDesignTimeContext");

                // Critical: Tell EF Core that Address is a value object, not an entity
                modelBuilder.Ignore<Address>();

                // Apply base configuration to get all entity mappings
                base.OnModelCreating(modelBuilder);

                // Add specific overrides for value objects to fix the error
                modelBuilder.Entity<Property>()
                    .Property(p => p.Address)
                    .HasColumnType("nvarchar(max)");

                modelBuilder.Entity<Tenant>()
                    .Property(t => t.PermanentAddress)
                    .HasColumnType("nvarchar(max)");

                modelBuilder.Entity<Tenant>()
                    .Property(t => t.IdDocument)
                    .HasColumnType("nvarchar(max)");

                Console.WriteLine("Model configured successfully");
            }
        }
    }
}
