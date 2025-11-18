namespace Sattim.Web.ViewModels.DashBoard
{
    public class MyBidsViewModel
    {
        public List<Models.Bid.Bid> Bids { get; set; } = new List<Models.Bid.Bid>();

        public string CurrentFilter { get; set; } = "all";

        public int AllCount { get; set; }
        public int ActiveCount { get; set; }
        public int WinningCount { get; set; }
        public int OutbidCount { get; set; }
        public int WonCount { get; set; }
        public int LostCount { get; set; }
    }
}