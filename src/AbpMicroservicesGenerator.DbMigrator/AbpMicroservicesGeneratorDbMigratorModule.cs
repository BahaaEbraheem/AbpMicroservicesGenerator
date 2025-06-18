using AbpMicroservicesGenerator.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AbpMicroservicesGenerator.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpMicroservicesGeneratorEntityFrameworkCoreModule),
    typeof(AbpMicroservicesGeneratorApplicationContractsModule)
    )]
public class AbpMicroservicesGeneratorDbMigratorModule : AbpModule
{
}
