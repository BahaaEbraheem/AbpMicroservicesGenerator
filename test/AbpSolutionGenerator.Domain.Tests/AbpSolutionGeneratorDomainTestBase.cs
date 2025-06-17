using Volo.Abp.Modularity;

namespace AbpSolutionGenerator;

/* Inherit from this class for your domain layer tests. */
public abstract class AbpSolutionGeneratorDomainTestBase<TStartupModule> : AbpSolutionGeneratorTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
