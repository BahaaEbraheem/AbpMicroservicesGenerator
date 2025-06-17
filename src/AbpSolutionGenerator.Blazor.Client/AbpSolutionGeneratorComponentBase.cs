using AbpSolutionGenerator.Localization;
using Volo.Abp.AspNetCore.Components;

namespace AbpSolutionGenerator.Blazor.Client;

public abstract class AbpSolutionGeneratorComponentBase : AbpComponentBase
{
    protected AbpSolutionGeneratorComponentBase()
    {
        LocalizationResource = typeof(AbpSolutionGeneratorResource);
    }
}
