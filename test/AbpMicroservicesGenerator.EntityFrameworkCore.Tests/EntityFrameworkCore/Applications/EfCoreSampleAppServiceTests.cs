using AbpMicroservicesGenerator.Samples;
using Xunit;

namespace AbpMicroservicesGenerator.EntityFrameworkCore.Applications;

[Collection(AbpMicroservicesGeneratorTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<AbpMicroservicesGeneratorEntityFrameworkCoreTestModule>
{

}
