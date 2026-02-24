namespace TalkToMe.Client.Services
{
    public class ToastService
    {
        public event Action<string, ToastLevel>? OnShow;

        public void ShowSuccess(string message) => OnShow?.Invoke(message, ToastLevel.Success);
        public void ShowError(string message) => OnShow?.Invoke(message, ToastLevel.Error);
        public void ShowInfo(string message) => OnShow?.Invoke(message, ToastLevel.Info);
        public void ShowWarning(string message) => OnShow?.Invoke(message, ToastLevel.Warning);
    }

    public class ToastModel
    {
        public string Message { get; set; } = "";
        public ToastLevel Level { get; set; } = ToastLevel.Info;
    }

    public enum ToastLevel { Success, Error, Info, Warning }
}
