using Volo.Abp.Settings;

namespace AbpMicroservicesGenerator.Settings;

public class AbpMicroservicesGeneratorSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AbpMicroservicesGeneratorSettings.MySetting1));
    }
}
