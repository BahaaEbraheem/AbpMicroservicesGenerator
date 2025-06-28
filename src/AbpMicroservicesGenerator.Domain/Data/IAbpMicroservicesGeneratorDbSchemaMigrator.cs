using System.Threading.Tasks;

namespace AbpMicroservicesGenerator.Data;

public interface IAbpMicroservicesGeneratorDbSchemaMigrator
{
    Task MigrateAsync();
}
