using System;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using MaintainEase.DbMigrator.Commands.Database;
using MaintainEase.DbMigrator.Commands.Migration;

namespace MaintainEase.DbMigrator.Commands
{
    /// <summary>
    /// Registers all commands with the Spectre.Console command app
    /// </summary>
    public static class CommandRegistrar
    {
        /// <summary>
        /// Register all commands with the provided command app
        /// </summary>
        public static void RegisterCommands(CommandApp app, IServiceProvider serviceProvider)
        {
            // Create a type registrar for dependency injection
            var registrar = new TypeRegistrar(serviceProvider);

            // Configure the command app with the type registrar
            app.Configure(config =>
            {
                // Set the application name and description
                config.SetApplicationName("MaintainEase.DbMigrator");
                config.SetApplicationVersion(typeof(CommandRegistrar).Assembly.GetName().Version?.ToString() ?? "1.0.0");

                // Register commands by category

                // Database commands
                config.AddCommand<StatusCommand>("status")
                    .WithDescription("Show database migration status")
                    .WithExample(new[] { "status", "--tenant", "CustomerA" });

                // Migration commands
                config.AddCommand<MigrateCommand>("migrate")
                    .WithDescription("Apply pending database migrations")
                    .WithExample(new[] { "migrate", "--environment", "Production", "--backup" });

                // Add CreateMigrationCommand 
                config.AddCommand<CreateMigrationCommand>("create")
                    .WithDescription("Create a new database migration")
                    .WithExample(new[] { "create", "AddUserTable", "--provider", "PostgreSQL" });
            });
        }
    }

    /// <summary>
    /// Type registrar for Spectre.Console.Cli that uses the .NET dependency injection container
    /// </summary>
    public class TypeRegistrar : ITypeRegistrar
    {
        private readonly IServiceProvider _serviceProvider;

        public TypeRegistrar(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ITypeResolver Build()
        {
            return new TypeResolver(_serviceProvider);
        }

        public void Register(Type service, Type implementation)
        {
            // Not needed when using an existing service provider
        }

        public void RegisterInstance(Type service, object implementation)
        {
            // Not needed when using an existing service provider
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            // Not needed when using an existing service provider
        }
    }

    /// <summary>
    /// Type resolver for Spectre.Console.Cli that uses the .NET dependency injection container
    /// </summary>
    public class TypeResolver : ITypeResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public TypeResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object Resolve(Type type)
        {
            return _serviceProvider.GetRequiredService(type);
        }
    }
}
