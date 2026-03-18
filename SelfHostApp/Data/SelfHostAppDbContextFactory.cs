using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SelfHostApp.Data;

public class SelfHostAppDbContextFactory : IDesignTimeDbContextFactory<SelfHostAppDbContext>
{
    public SelfHostAppDbContext CreateDbContext(string[] args)
    {
        SelfHostAppGlobalFeatureConfigurator.Configure();
        SelfHostAppModuleExtensionConfigurator.Configure();

        SelfHostAppEfCoreEntityExtensionMappings.Configure();
        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<SelfHostAppDbContext>()
            .UseSqlite(configuration.GetConnectionString("Default"));

        return new SelfHostAppDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}