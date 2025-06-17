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
using Volo.Abp.PermissionManagement.Blazor.Components;

namespace AbpSolutionGenerator.Blazor.Client.Pages.Identity;

public partial class CustomUserManagement
{
    protected List<IdentityUserDto> Users = new();
    protected int TotalCount;
    protected int PageSize = 10;
    protected int CurrentPage = 1;
    protected string CurrentSorting = string.Empty;
    protected GetIdentityUsersInput Filter = new();

    protected IdentityUserCreateDto NewUser = new();
    protected Guid? EditingUserId;
    protected Modal UserModal = new();
    protected Validations UserValidationsRef = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await GetUsersAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    protected virtual async Task GetUsersAsync()
    {
        Filter.MaxResultCount = PageSize;
        Filter.SkipCount = (CurrentPage - 1) * PageSize;
        Filter.Sorting = CurrentSorting;

        var result = await UserAppService.GetListAsync(Filter);
        Users = result.Items.ToList();
        TotalCount = (int)result.TotalCount;
    }

    protected virtual async Task OnDataGridReadAsync(DataGridReadDataEventArgs<IdentityUserDto> e)
    {
        CurrentSorting = e.Columns
            .Where(c => c.SortDirection != SortDirection.Default)
            .Select(c => c.Field + (c.SortDirection == SortDirection.Descending ? " DESC" : ""))
            .JoinAsString(",");
        CurrentPage = e.Page;

        await GetUsersAsync();

        await InvokeAsync(StateHasChanged);
    }

    protected virtual void OpenCreateUserModal()
    {
        NewUser = new IdentityUserCreateDto();
        EditingUserId = null;
        UserModal.Show();
    }

    protected virtual async Task OpenEditUserModal(IdentityUserDto user)
    {
        try
        {
            var userDto = await UserAppService.GetAsync(user.Id);

            EditingUserId = user.Id;
            NewUser = ObjectMapper.Map<IdentityUserDto, IdentityUserCreateDto>(userDto);
            UserModal.Show();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    protected virtual void CloseUserModal()
    {
        UserModal.Hide();
    }

    protected virtual async Task CreateOrUpdateUser()
    {
        try
        {
            if (await UserValidationsRef.ValidateAll())
            {
                if (EditingUserId == null)
                {
                    await UserAppService.CreateAsync(NewUser);
                }
                else
                {
                    var updateDto = ObjectMapper.Map<IdentityUserCreateDto, IdentityUserUpdateDto>(NewUser);
                    await UserAppService.UpdateAsync(EditingUserId.Value, updateDto);
                }

                await GetUsersAsync();
                UserModal.Hide();
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    protected virtual async Task DeleteUser(IdentityUserDto user)
    {
        var confirmMessage = L["UserDeletionConfirmationMessage", user.UserName];
        if (await Message.Confirm(confirmMessage))
        {
            await UserAppService.DeleteAsync(user.Id);
            await GetUsersAsync();
        }
    }

    protected virtual async Task OpenPermissionsModal(IdentityUserDto user)
    {
        // يمكنك إضافة منطق فتح modal للصلاحيات هنا
        await Message.Info($"فتح صلاحيات المستخدم: {user.UserName}");
    }
}
