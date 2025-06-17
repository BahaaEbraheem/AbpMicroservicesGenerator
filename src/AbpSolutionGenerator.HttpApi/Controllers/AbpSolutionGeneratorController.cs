using AbpSolutionGenerator.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AbpSolutionGenerator.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AbpSolutionGeneratorController : AbpControllerBase
{
    protected AbpSolutionGeneratorController()
    {
        LocalizationResource = typeof(AbpSolutionGeneratorResource);
    }
}
