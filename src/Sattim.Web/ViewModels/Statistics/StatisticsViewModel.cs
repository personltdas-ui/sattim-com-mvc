namespace Sattim.Web.ViewModels.Statistics
{
    public class StatisticsViewModel
    {
        public int TotalAuctions { get; set; }
        public int ActiveAuctions { get; set; }
        public int CompletedAuctions { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageAuctionValue { get; set; }
        public int TotalBids { get; set; }
        public double AverageBidsPerAuction { get; set; }

        public List<CategoryStatistic> CategoryStatistics { get; set; } = new List<CategoryStatistic>();
        public List<MonthlyStatistic> MonthlyStatistics { get; set; } = new List<MonthlyStatistic>();
    }

    public class CategoryStatistic
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBids { get; set; }
    }

    public class MonthlyStatistic
    {
        public string Month { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal Revenue { get; set; }
        public int BidCount { get; set; }
    }
}
