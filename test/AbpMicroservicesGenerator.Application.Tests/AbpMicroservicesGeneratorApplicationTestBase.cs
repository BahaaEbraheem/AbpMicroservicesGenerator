using Volo.Abp.Modularity;

namespace AbpMicroservicesGenerator;

public abstract class AbpMicroservicesGeneratorApplicationTestBase<TStartupModule> : AbpMicroservicesGeneratorTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
