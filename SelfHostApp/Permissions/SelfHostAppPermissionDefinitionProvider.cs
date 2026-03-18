using SelfHostApp.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace SelfHostApp.Permissions;

public class SelfHostAppPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(SelfHostAppPermissions.GroupName);



        //Define your own permissions here. Example:
        //myGroup.AddPermission(SelfHostAppPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<SelfHostAppResource>(name);
    }
}
