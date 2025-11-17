namespace Sattim.Web.ViewModels.Shared
{
    public class BreadcrumbViewModel
    {
        public List<BreadcrumbItem> Items { get; set; } = new List<BreadcrumbItem>();
    }

    public class BreadcrumbItem
    {
        public string Text { get; set; } = string.Empty;
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public object? RouteValues { get; set; }
        public bool IsActive { get; set; }
    }
}
