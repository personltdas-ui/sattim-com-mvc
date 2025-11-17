namespace Sattim.Web.ViewModels.DashBoard
{
    public class SalesViewModel
    {
        // Ana veri
        public List<Models.Product.Product> SoldProducts { get; set; }

        // ViewBag verileri
        public decimal TotalRevenue { get; set; }
        public List<string> ChartLabels { get; set; }
        public List<decimal> ChartData { get; set; }

        public SalesViewModel()
        {
            SoldProducts = new List<Models.Product.Product>();
            ChartLabels = new List<string>();
            ChartData = new List<decimal>();
        }
    }
}
