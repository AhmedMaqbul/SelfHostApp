namespace SelfHostApp.Permissions;

public static class TodoPermissions
{
    public const string GroupName = "Todo";

    public static class Items
    {
        public const string Default = GroupName + ".Items";
        public const string Create = Default + ".Create";
        public const string Delete = Default + ".Delete";
    }
}
