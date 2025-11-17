namespace Sattim.Web.ViewModels.DashBoard
{
    public class SalesPageViewModel
    {
        // İstatistik Kartları için
        public int TotalSalesCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal AverageSale { get; set; }

        // Grafik için
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<decimal> ChartData { get; set; } = new List<decimal>();

        // Tablo için
        public List<SalesItemViewModel> SalesList { get; set; } = new List<SalesItemViewModel>();
    }
}
