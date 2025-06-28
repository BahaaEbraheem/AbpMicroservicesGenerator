using System;
using System.Collections.Generic;
using System.Text;
using AbpMicroservicesGenerator.Localization;
using Volo.Abp.Application.Services;

namespace AbpMicroservicesGenerator;

/* Inherit your application services from this class.
 */
public abstract class AbpMicroservicesGeneratorAppService : ApplicationService
{
    protected AbpMicroservicesGeneratorAppService()
    {
        LocalizationResource = typeof(AbpMicroservicesGeneratorResource);
    }
}
