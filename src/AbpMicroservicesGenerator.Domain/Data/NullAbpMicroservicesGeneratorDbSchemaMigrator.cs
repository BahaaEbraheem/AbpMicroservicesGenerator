using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AbpMicroservicesGenerator.Data;

/* This is used if database provider does't define
 * IAbpMicroservicesGeneratorDbSchemaMigrator implementation.
 */
public class NullAbpMicroservicesGeneratorDbSchemaMigrator : IAbpMicroservicesGeneratorDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
