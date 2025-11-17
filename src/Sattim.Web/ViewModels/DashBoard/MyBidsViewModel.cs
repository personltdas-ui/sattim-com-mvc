namespace Sattim.Web.ViewModels.DashBoard
{
    public class MyBidsViewModel
    {
        /// <summary>
        /// Filtrelenmiş tekliflerin listesi
        /// </summary>
        public List<Models.Bid.Bid> Bids { get; set; } = new List<Models.Bid.Bid>();

        /// <summary>
        /// O anda seçili olan filtre ("all", "active", "won" vb.)
        /// </summary>
        public string CurrentFilter { get; set; } = "all";

        // Filtre sekmeleri için sayım sonuçları
        public int AllCount { get; set; }
        public int ActiveCount { get; set; }
        public int WinningCount { get; set; }
        public int OutbidCount { get; set; }
        public int WonCount { get; set; }
        public int LostCount { get; set; }
    }
}
