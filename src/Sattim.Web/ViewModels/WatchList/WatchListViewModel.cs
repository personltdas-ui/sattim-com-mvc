namespace Sattim.Web.ViewModels.WatchList
{
    public class WatchListViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string ProductImageUrl { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime EndDate { get; set; }
        public int BidCount { get; set; }
        public bool NotifyOnNewBid { get; set; }
        public bool NotifyBeforeEnd { get; set; }
        public DateTime AddedDate { get; set; }
        public string TimeRemaining { get; set; }
    }
}
