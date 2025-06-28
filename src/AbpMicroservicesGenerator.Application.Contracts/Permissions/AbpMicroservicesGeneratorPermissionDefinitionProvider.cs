using AbpMicroservicesGenerator.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AbpMicroservicesGenerator.Permissions;

public class AbpMicroservicesGeneratorPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AbpMicroservicesGeneratorPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AbpMicroservicesGeneratorPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AbpMicroservicesGeneratorResource>(name);
    }
}
