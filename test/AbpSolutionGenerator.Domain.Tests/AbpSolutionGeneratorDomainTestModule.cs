using Volo.Abp.Modularity;

namespace AbpSolutionGenerator;

[DependsOn(
    typeof(AbpSolutionGeneratorDomainModule),
    typeof(AbpSolutionGeneratorTestBaseModule)
)]
public class AbpSolutionGeneratorDomainTestModule : AbpModule
{

}
