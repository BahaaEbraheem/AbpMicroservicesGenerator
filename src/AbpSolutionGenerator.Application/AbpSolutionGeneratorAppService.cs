using System;
using System.Collections.Generic;
using System.Text;
using AbpSolutionGenerator.Localization;
using Volo.Abp.Application.Services;

namespace AbpSolutionGenerator;

/* Inherit your application services from this class.
 */
public abstract class AbpSolutionGeneratorAppService : ApplicationService
{
    protected AbpSolutionGeneratorAppService()
    {
        LocalizationResource = typeof(AbpSolutionGeneratorResource);
    }
}
