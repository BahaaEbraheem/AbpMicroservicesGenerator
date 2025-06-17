using Volo.Abp.Modularity;

namespace AbpSolutionGenerator;

[DependsOn(
    typeof(AbpSolutionGeneratorApplicationModule),
    typeof(AbpSolutionGeneratorDomainTestModule)
)]
public class AbpSolutionGeneratorApplicationTestModule : AbpModule
{

}
