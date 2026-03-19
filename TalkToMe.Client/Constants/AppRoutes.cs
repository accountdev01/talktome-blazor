namespace TalkToMe.Client.Constants
{
    public static class AppRoutes
    {
        public const string Home = "";

<<<<<<< HEAD
        public const string Dashboard = "management";
=======
        public const string Dashboard = "dashboard";
>>>>>>> c5e64a8 (Implement user login system)

        public const string Request = "request";

        public const string Workflow = "workflow";

        public const string Configurations = "configurations";

        public const string UserEdit = "user/edit/{id:int}";
        public static string GetUserEditUrl(int id) => $"user/edit/{id}";
    }
}
