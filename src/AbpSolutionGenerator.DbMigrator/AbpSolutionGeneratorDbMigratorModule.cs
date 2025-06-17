using AbpSolutionGenerator.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AbpSolutionGenerator.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpSolutionGeneratorEntityFrameworkCoreModule),
    typeof(AbpSolutionGeneratorApplicationContractsModule)
    )]
public class AbpSolutionGeneratorDbMigratorModule : AbpModule
{
}
