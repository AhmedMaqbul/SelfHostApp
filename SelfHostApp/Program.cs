using System;
using Microsoft.Extensions.Hosting.WindowsServices;
using SelfHostApp.Data;
using Serilog;
using Serilog.Events;
using Volo.Abp.Data;

namespace SelfHostApp;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "logs-.txt");

        var loggerConfiguration = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true
            ));

        if (!WindowsServiceHelpers.IsWindowsService())
        {
            loggerConfiguration.WriteTo.Console();
        }

        if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
        {
            loggerConfiguration.WriteTo.EventLog(
                source: "SelfHostApp",
                manageEventSource: true,
                restrictedToMinimumLevel: LogEventLevel.Information
            );
        }

        if (IsMigrateDatabase(args))
        {
            loggerConfiguration.MinimumLevel.Override("Volo.Abp", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseKestrel();

            builder.Host.UseWindowsService()
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();

            if (IsMigrateDatabase(args))
            {
                builder.Services.AddDataMigrationEnvironment();
            }
            await builder.AddApplicationAsync<SelfHostAppModule>();
            var app = builder.Build();

            if (IsMigrateDatabase(args))
            {
                using var scope = app.Services.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<SelfHostAppDbMigrationService>();

                await migrationService.MigrateAsync();

                var previous = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Migration completed.");
                Console.ForegroundColor = previous;

                return 0;
            }

            await app.InitializeApplicationAsync();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapControllers();

            app.MapGet("/", () => Results.Redirect("/app"));

            app.MapFallbackToFile(
                "/app/{*path:nonfile}",
                "app/index.html"
            );

            Log.Information("Starting SelfHostApp.");
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "SelfHostApp terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static bool IsMigrateDatabase(string[] args)
    {
        return args.Any(x => x.Contains("--migrate-database", StringComparison.OrdinalIgnoreCase));
    }
}
