namespace Sattim.Web.ViewModels.DashBoard
{
    public class SalesPageViewModel
    {
        public int TotalSalesCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal AverageSale { get; set; }

        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<decimal> ChartData { get; set; } = new List<decimal>();

        public List<SalesItemViewModel> SalesList { get; set; } = new List<SalesItemViewModel>();
    }
}