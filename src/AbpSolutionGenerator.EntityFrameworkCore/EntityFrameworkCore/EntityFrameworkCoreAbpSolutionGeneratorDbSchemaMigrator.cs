using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AbpSolutionGenerator.Data;
using Volo.Abp.DependencyInjection;

namespace AbpSolutionGenerator.EntityFrameworkCore;

public class EntityFrameworkCoreAbpSolutionGeneratorDbSchemaMigrator
    : IAbpSolutionGeneratorDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreAbpSolutionGeneratorDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the AbpSolutionGeneratorDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AbpSolutionGeneratorDbContext>()
            .Database
            .MigrateAsync();
    }
}
