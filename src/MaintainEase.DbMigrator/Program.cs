using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using MaintainEase.DbMigrator.Commands;
using MaintainEase.DbMigrator.Configuration;
using MaintainEase.DbMigrator.Services;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;
using MaintainEase.DbMigrator.UI.Theme;

namespace MaintainEase.DbMigrator;

public class Program
{
    // Add static property to access the service provider globally
    public static ServiceProvider ServiceProvider { get; private set; }

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Set console title
            Console.Title = "MaintainEase DB Migrator";

            // Create configuration
            var configuration = BuildConfiguration();

            // Configure services
            ServiceProvider = ConfigureServices(configuration);

            // Get application context
            var appContext = ServiceProvider.GetRequiredService<ApplicationContext>();
            appContext.Initialize();

            // Determine if we're in CLI mode or interactive mode
            bool isCliMode = args.Length > 0;

            if (isCliMode)
            {
                // CLI mode - run the specified command
                return await RunCommandLineMode(args, ServiceProvider);
            }
            else
            {
                // Interactive mode - run the menu system
                await RunInteractiveMode(ServiceProvider);
                return 0;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables("MAINTAINEASE_")
            .Build();
    }

    private static ServiceProvider ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton(configuration);

        // Add logging
        services.AddLogging(logging =>
        {
            logging.AddConfiguration(configuration.GetSection("Logging"));
            logging.AddConsole();
            logging.AddDebug();
            logging.AddEventSourceLogger();

            // Add file logging
            var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
        });

        // Add application context
        services.AddSingleton<ApplicationContext>();

        // Add services
        services.AddTransient<MenuService>();

        // Add command classes
        services.AddTransient<MaintainEase.DbMigrator.Commands.Database.StatusCommand>();
        services.AddTransient<MaintainEase.DbMigrator.Commands.Migration.MigrateCommand>();

        // Build service provider
        return services.BuildServiceProvider();
    }

    private static async Task<int> RunCommandLineMode(string[] args, ServiceProvider serviceProvider)
    {
        // Create and configure the command app
        var app = new CommandApp();
        CommandRegistrar.RegisterCommands(app, serviceProvider);

        // Run the command app
        return await app.RunAsync(args);
    }

    private static async Task RunInteractiveMode(ServiceProvider serviceProvider)
    {
        try
        {
            // Show startup spinner
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(new Style(Color.Blue))
                .StartAsync("Initializing MaintainEase DB Migrator...", async ctx =>
                {
                    await Task.Delay(1500); // Visual delay for better UX
                });

            // Get the menu service and run it
            var menuService = serviceProvider.GetRequiredService<MenuService>();
            await menuService.RunAsync();
        }
        catch (Exception ex)
        {
            // Get logger and log the error
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error starting interactive mode");

            // Show error to user
            Console.Clear();
            AnsiConsole.MarkupLine("[bold red]ERROR:[/] An error occurred while starting the application");

            if (serviceProvider.GetService<ApplicationContext>()?.IsDebugMode == true)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShowLinks);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                AnsiConsole.MarkupLine("[grey]Run with --debug flag for more details[/]");
            }

            // Pause so user can see the error
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}
