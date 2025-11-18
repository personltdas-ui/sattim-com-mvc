using Sattim.Web.Models.Product;

namespace Sattim.Web.ViewModels.Bid
{
    
    public class ProductBidHistoryViewModel
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public decimal CurrentPrice { get; set; }
        public int BidCount { get; set; }
        public DateTime EndDate { get; set; }
        public string PrimaryImageUrl { get; set; }

        // Teklif listesi
        public List<BidHistoryItemViewModel> Bids { get; set; } = new List<BidHistoryItemViewModel>();
    }
}
