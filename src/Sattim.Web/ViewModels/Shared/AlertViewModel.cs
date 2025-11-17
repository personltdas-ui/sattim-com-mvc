namespace Sattim.Web.ViewModels.Shared
{
    public class AlertViewModel
    {
        public AlertType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Title { get; set; }
        public bool Dismissible { get; set; } = true;
    }

    public enum AlertType
    {
        Success,
        Info,
        Warning,
        Danger
    }
}
