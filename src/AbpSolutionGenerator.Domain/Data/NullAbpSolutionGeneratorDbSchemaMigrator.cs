using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AbpSolutionGenerator.Data;

/* This is used if database provider does't define
 * IAbpSolutionGeneratorDbSchemaMigrator implementation.
 */
public class NullAbpSolutionGeneratorDbSchemaMigrator : IAbpSolutionGeneratorDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
