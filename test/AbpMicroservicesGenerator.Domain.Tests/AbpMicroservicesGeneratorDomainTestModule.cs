using Volo.Abp.Modularity;

namespace AbpMicroservicesGenerator;

[DependsOn(
    typeof(AbpMicroservicesGeneratorDomainModule),
    typeof(AbpMicroservicesGeneratorTestBaseModule)
)]
public class AbpMicroservicesGeneratorDomainTestModule : AbpModule
{

}
