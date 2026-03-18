using SelfHostApp.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace SelfHostApp.Permissions;

public class TodoPermissionDefinitionProvider
    : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var todoGroup = context.AddGroup(
            TodoPermissions.GroupName,
            L("Permission:Todo")
        );

        var itemsPermission = todoGroup.AddPermission(
            TodoPermissions.Items.Default,
            L("Permission:TodoItems")
        );

        itemsPermission.AddChild(
            TodoPermissions.Items.Create,
            L("Permission:TodoItemsCreate")
        );

        itemsPermission.AddChild(
            TodoPermissions.Items.Delete,
            L("Permission:TodoItemsDelete")
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<SelfHostAppResource>(name);
    }
}
