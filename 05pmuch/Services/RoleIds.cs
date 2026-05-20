namespace _05pmuch.Services
{
    public static class RoleIds
    {
        public const int Admin = 1;
        public const int Operator = 2;
        public const int User = 3;

        public static string GetName(int roleId)
        {
            switch (roleId)
            {
                case Admin: return "admin";
                case Operator: return "operator";
                case User: return "user";
                default: return "user";
            }
        }
    }
}
