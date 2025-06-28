using AbpMicroservicesGenerator.Samples;
using Xunit;

namespace AbpMicroservicesGenerator.EntityFrameworkCore.Domains;

[Collection(AbpMicroservicesGeneratorTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<AbpMicroservicesGeneratorEntityFrameworkCoreTestModule>
{

}
