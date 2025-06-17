using AbpSolutionGenerator.Samples;
using Xunit;

namespace AbpSolutionGenerator.EntityFrameworkCore.Applications;

[Collection(AbpSolutionGeneratorTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<AbpSolutionGeneratorEntityFrameworkCoreTestModule>
{

}
