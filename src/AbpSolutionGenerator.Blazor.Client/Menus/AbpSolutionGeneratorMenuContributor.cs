using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AbpSolutionGenerator.Localization;
using AbpSolutionGenerator.MultiTenancy;
using Volo.Abp.Account.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity.Blazor;
using Volo.Abp.SettingManagement.Blazor.Menus;
using Volo.Abp.TenantManagement.Blazor.Navigation;
using Volo.Abp.UI.Navigation;

namespace AbpSolutionGenerator.Blazor.Client.Menus;

public class AbpSolutionGeneratorMenuContributor : IMenuContributor
{
    private readonly IConfiguration _configuration;

    public AbpSolutionGeneratorMenuContributor(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
        else if (context.Menu.Name == StandardMenus.User)
        {
            await ConfigureUserMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<AbpSolutionGeneratorResource>();

        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                AbpSolutionGeneratorMenus.Home,
                l["Menu:Home"],
                "/",
                icon: "fas fa-home"
            )
        );

        context.Menu.Items.Insert(
            1,
            new ApplicationMenuItem(
                AbpSolutionGeneratorMenus.SolutionGenerator,
                l["Menu:SolutionGenerator"],
                "/solution-generator",
                icon: "fas fa-cogs"
            )
        );

        // إضافة رابط تسجيل الدخول للمستخدمين غير المسجلين
        context.Menu.Items.Insert(
            2,
            new ApplicationMenuItem(
                "Account.Login",
                l["Login"],
                "/account/login",
                icon: "fas fa-sign-in-alt"
            ).RequireAuthenticated()
        );

        var administration = context.Menu.GetAdministration();

        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }
        else
        {
            administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
        }

        // إضافة قائمة Identity المخصصة
        var customIdentityMenu = new ApplicationMenuItem(
            "CustomIdentity",
            l["Menu:CustomIdentity"],
            icon: "fas fa-users"
        );

        customIdentityMenu.AddItem(new ApplicationMenuItem(
            "CustomIdentity.Users",
            l["Menu:CustomUsers"],
            "/identity/users-custom",
            icon: "fas fa-user"
        ));

        customIdentityMenu.AddItem(new ApplicationMenuItem(
            "CustomIdentity.Roles",
            l["Menu:CustomRoles"],
            "/identity/roles-custom",
            icon: "fas fa-user-tag"
        ));

        administration.AddItem(customIdentityMenu);

        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenus.GroupName, 3);

        return Task.CompletedTask;
    }

    private Task ConfigureUserMenuAsync(MenuConfigurationContext context)
    {
        var accountStringLocalizer = context.GetLocalizer<AccountResource>();

        var authServerUrl = _configuration["AuthServer:Authority"] ?? "";

        context.Menu.AddItem(new ApplicationMenuItem(
            "Account.Manage",
            accountStringLocalizer["MyAccount"],
            $"{authServerUrl.EnsureEndsWith('/')}Account/Manage",
            icon: "fa fa-cog",
            order: 1000,
            target: "_blank").RequireAuthenticated());

        return Task.CompletedTask;
    }
}
