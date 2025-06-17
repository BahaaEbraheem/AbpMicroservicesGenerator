using Volo.Abp.Modularity;

namespace AbpSolutionGenerator;

public abstract class AbpSolutionGeneratorApplicationTestBase<TStartupModule> : AbpSolutionGeneratorTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
