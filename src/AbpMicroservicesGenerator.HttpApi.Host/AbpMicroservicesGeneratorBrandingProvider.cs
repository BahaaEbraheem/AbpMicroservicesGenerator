using Microsoft.Extensions.Localization;
using AbpMicroservicesGenerator.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace AbpMicroservicesGenerator;

[Dependency(ReplaceServices = true)]
public class AbpMicroservicesGeneratorBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<AbpMicroservicesGeneratorResource> _localizer;

    public AbpMicroservicesGeneratorBrandingProvider(IStringLocalizer<AbpMicroservicesGeneratorResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
