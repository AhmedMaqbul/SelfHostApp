using Volo.Abp.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace SelfHostApp.Data;

public class SelfHostAppDbSchemaMigrator : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public SelfHostAppDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        
        /* We intentionally resolving the SelfHostAppDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<SelfHostAppDbContext>()
            .Database
            .MigrateAsync();

    }
}
