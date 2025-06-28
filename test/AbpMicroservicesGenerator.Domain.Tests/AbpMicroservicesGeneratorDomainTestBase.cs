using Volo.Abp.Modularity;

namespace AbpMicroservicesGenerator;

/* Inherit from this class for your domain layer tests. */
public abstract class AbpMicroservicesGeneratorDomainTestBase<TStartupModule> : AbpMicroservicesGeneratorTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
