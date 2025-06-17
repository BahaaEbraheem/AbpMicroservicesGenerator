using AbpSolutionGenerator.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AbpSolutionGenerator.Permissions;

public class AbpSolutionGeneratorPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AbpSolutionGeneratorPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AbpSolutionGeneratorPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AbpSolutionGeneratorResource>(name);
    }
}
