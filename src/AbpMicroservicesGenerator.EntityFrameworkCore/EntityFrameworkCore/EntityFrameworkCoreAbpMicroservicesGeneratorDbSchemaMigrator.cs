using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AbpMicroservicesGenerator.Data;
using Volo.Abp.DependencyInjection;

namespace AbpMicroservicesGenerator.EntityFrameworkCore;

public class EntityFrameworkCoreAbpMicroservicesGeneratorDbSchemaMigrator
    : IAbpMicroservicesGeneratorDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreAbpMicroservicesGeneratorDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the AbpMicroservicesGeneratorDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AbpMicroservicesGeneratorDbContext>()
            .Database
            .MigrateAsync();
    }
}
