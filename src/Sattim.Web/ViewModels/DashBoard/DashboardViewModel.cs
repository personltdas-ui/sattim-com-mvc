namespace Sattim.Web.ViewModels.DashBoard
{
    public class DashboardViewModel
    {
        // İstatistikler
        public int MyProducts { get; set; }
        public int MyActiveProducts { get; set; }
        public int MySoldProducts { get; set; }
        public int MyBids { get; set; }
        public int MyWinnings { get; set; }
        public int MyFavorites { get; set; }

        // Cüzdan
        public decimal WalletBalance { get; set; }

        // Son Aktiviteler
        public List<Models.Product.Product> RecentProducts { get; set; }
        public List<Models.Bid.Bid> RecentBids { get; set; }

        public DashboardViewModel()
        {
            RecentProducts = new List<Models.Product.Product>();
            RecentBids = new List<Models.Bid.Bid>();
        }
    }
}
