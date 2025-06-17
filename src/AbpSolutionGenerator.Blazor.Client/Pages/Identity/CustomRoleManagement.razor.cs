using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using Blazorise.DataGrid;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Components.Web.Extensibility.EntityActions;
using Volo.Abp.AspNetCore.Components.Web.Extensibility.TableColumns;
using Volo.Abp.AspNetCore.Components.Web.Theming.PageToolbars;
using Volo.Abp.BlazoriseUI.Components;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;

namespace AbpSolutionGenerator.Blazor.Client.Pages.Identity;

public partial class CustomRoleManagement
{
    protected List<IdentityRoleDto> Roles = new();
    protected int TotalCount;
    protected int PageSize = 10;
    protected int CurrentPage = 1;
    protected string CurrentSorting = string.Empty;
    protected GetIdentityRolesInput Filter = new();

    protected IdentityRoleCreateDto NewRole = new();
    protected Guid? EditingRoleId;
    protected Modal RoleModal = new();
    protected Validations RoleValidationsRef = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await GetRolesAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    protected virtual async Task GetRolesAsync()
    {
        Filter.MaxResultCount = PageSize;
        Filter.SkipCount = (CurrentPage - 1) * PageSize;
        Filter.Sorting = CurrentSorting;

        var result = await RoleAppService.GetListAsync(Filter);
        Roles = result.Items.ToList();
        TotalCount = (int)result.TotalCount;
    }

    protected virtual async Task OnDataGridReadAsync(DataGridReadDataEventArgs<IdentityRoleDto> e)
    {
        CurrentSorting = e.Columns
            .Where(c => c.SortDirection != SortDirection.Default)
            .Select(c => c.Field + (c.SortDirection == SortDirection.Descending ? " DESC" : ""))
            .JoinAsString(",");
        CurrentPage = e.Page;

        await GetRolesAsync();

        await InvokeAsync(StateHasChanged);
    }

    protected virtual void OpenCreateRoleModal()
    {
        NewRole = new IdentityRoleCreateDto();
        EditingRoleId = null;
        RoleModal.Show();
    }

    protected virtual async Task OpenEditRoleModal(IdentityRoleDto role)
    {
        try
        {
            var roleDto = await RoleAppService.GetAsync(role.Id);

            EditingRoleId = role.Id;
            NewRole = ObjectMapper.Map<IdentityRoleDto, IdentityRoleCreateDto>(roleDto);
            RoleModal.Show();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    protected virtual void CloseRoleModal()
    {
        RoleModal.Hide();
    }

    protected virtual async Task CreateOrUpdateRole()
    {
        try
        {
            if (await RoleValidationsRef.ValidateAll())
            {
                if (EditingRoleId == null)
                {
                    await RoleAppService.CreateAsync(NewRole);
                }
                else
                {
                    var updateDto = ObjectMapper.Map<IdentityRoleCreateDto, IdentityRoleUpdateDto>(NewRole);
                    await RoleAppService.UpdateAsync(EditingRoleId.Value, updateDto);
                }

                await GetRolesAsync();
                RoleModal.Hide();
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    protected virtual async Task DeleteRole(IdentityRoleDto role)
    {
        var confirmMessage = L["RoleDeletionConfirmationMessage", role.Name];
        if (await Message.Confirm(confirmMessage))
        {
            await RoleAppService.DeleteAsync(role.Id);
            await GetRolesAsync();
        }
    }

    protected virtual async Task OpenPermissionsModal(IdentityRoleDto role)
    {
        // يمكنك إضافة منطق فتح modal للصلاحيات هنا
        await Message.Info($"فتح صلاحيات الدور: {role.Name}");
    }
}
