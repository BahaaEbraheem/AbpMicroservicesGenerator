using AbpMicroservicesGenerator.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AbpMicroservicesGenerator.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AbpMicroservicesGeneratorController : AbpControllerBase
{
    protected AbpMicroservicesGeneratorController()
    {
        LocalizationResource = typeof(AbpMicroservicesGeneratorResource);
    }
}
