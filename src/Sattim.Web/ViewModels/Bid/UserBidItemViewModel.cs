using Sattim.Web.Models.Product;

namespace Sattim.Web.ViewModels.Bid
{
    /// <summary>
    /// "Tekliflerim" sayfasındaki bir ihaleyi temsil eder.
    /// (GetUserBidsAsync tarafından döndürülür)
    /// </summary>
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

    /// <summary>
    /// "Tekliflerim" sayfasındaki bir ihalenin durumunu belirtir.
    /// </summary>
    public enum UserBidStatus
    {
        Active_Winning, // Aktif - Kazanıyorsun
        Active_Losing,  // Aktif - Kaybediyorsun
        Won,            // Bitti - Kazandın
        Lost            // Bitti - Kaybettin
    }
}
