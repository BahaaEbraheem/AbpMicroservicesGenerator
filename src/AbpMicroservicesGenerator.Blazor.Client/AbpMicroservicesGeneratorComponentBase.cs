using AbpMicroservicesGenerator.Localization;
using Volo.Abp.AspNetCore.Components;

namespace AbpMicroservicesGenerator.Blazor.Client;

public abstract class AbpMicroservicesGeneratorComponentBase : AbpComponentBase
{
    protected AbpMicroservicesGeneratorComponentBase()
    {
        LocalizationResource = typeof(AbpMicroservicesGeneratorResource);
    }
}
