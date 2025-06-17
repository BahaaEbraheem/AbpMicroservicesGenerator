using System.Threading.Tasks;

namespace AbpSolutionGenerator.Data;

public interface IAbpSolutionGeneratorDbSchemaMigrator
{
    Task MigrateAsync();
}
