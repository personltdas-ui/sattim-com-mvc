using Sattim.Web.Models.Product;

namespace Sattim.Web.ViewModels.Bid
{
    public class UserBidItemViewModel
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string PrimaryImageUrl { get; set; }
        public DateTime EndDate { get; set; }

        public decimal CurrentPrice { get; set; }
        public decimal MyHighestBid { get; set; }
        public UserBidStatus Status { get; set; }
    }

    public enum UserBidStatus
    {
        Active_Winning,
        Active_Losing,
        Won,
        Lost
    }
}