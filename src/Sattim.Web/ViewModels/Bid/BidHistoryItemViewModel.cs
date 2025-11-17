namespace Sattim.Web.ViewModels.Bid
{
    /// <summary>
    /// Teklif geçmişindeki tek bir satırı temsil eder.
    /// </summary>
    public class BidHistoryItemViewModel
    {
        public string BidderFullName { get; set; }
        public decimal Amount { get; set; }
        public DateTime BidDate { get; set; }
    }
}
