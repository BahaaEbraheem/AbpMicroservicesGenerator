using Volo.Abp.Settings;

namespace AbpSolutionGenerator.Settings;

public class AbpSolutionGeneratorSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AbpSolutionGeneratorSettings.MySetting1));
    }
}
