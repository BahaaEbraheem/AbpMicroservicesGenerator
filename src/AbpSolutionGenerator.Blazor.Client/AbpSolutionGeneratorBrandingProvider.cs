using Microsoft.Extensions.Localization;
using AbpSolutionGenerator.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace AbpSolutionGenerator.Blazor.Client;

[Dependency(ReplaceServices = true)]
public class AbpSolutionGeneratorBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<AbpSolutionGeneratorResource> _localizer;

    public AbpSolutionGeneratorBrandingProvider(IStringLocalizer<AbpSolutionGeneratorResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
