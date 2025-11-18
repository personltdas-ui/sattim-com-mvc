namespace Sattim.Web.ViewModels.DashBoard
{
    public class MyWinningsViewModel
    {
        public List<WonProductViewModel> WonProducts { get; set; } = new List<WonProductViewModel>();

        public decimal TotalSpent { get; set; }

        public int ItemsPaid { get; set; }

        public int ItemsPending { get; set; }

        public int TotalWinnings { get; set; }
    }
}