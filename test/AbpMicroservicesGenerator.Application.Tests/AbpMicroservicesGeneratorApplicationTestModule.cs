using Volo.Abp.Modularity;

namespace AbpMicroservicesGenerator;

[DependsOn(
    typeof(AbpMicroservicesGeneratorApplicationModule),
    typeof(AbpMicroservicesGeneratorDomainTestModule)
)]
public class AbpMicroservicesGeneratorApplicationTestModule : AbpModule
{

}
