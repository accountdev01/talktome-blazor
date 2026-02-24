namespace TalkToMe.Client.Constants
{
    public static class AppRoutes
    {
        public const string Home = "";

        public const string Dashboard = "management";

        public const string Request = "request";

        public const string Workflow = "workflow";

        public const string Configurations = "configurations";

        public const string UserEdit = "user/edit/{id:int}";
        public static string GetUserEditUrl(int id) => $"user/edit/{id}";
    }
}
