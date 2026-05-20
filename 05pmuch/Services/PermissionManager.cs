namespace _05pmuch.Services
{
    public static class PermissionManager
    {
        public static bool CanEditData { get; private set; }

        public static string CurrentRole { get; private set; } = string.Empty;

        public static void SetRole(string roleName)
        {
            CurrentRole = roleName ?? string.Empty;
            switch (CurrentRole.ToLowerInvariant())
            {
                case "admin":
                case "operator":
                    CanEditData = true;
                    break;
                default:
                    CanEditData = false;
                    break;
            }
        }
    }
}
