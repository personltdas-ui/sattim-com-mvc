namespace Sattim.Web.ViewModels.Bid
{
    
    public class AutoBidSettingViewModel
    {
        public int ProductId { get; set; }
        public string UserId { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal IncrementAmount { get; set; }
        public bool IsActive { get; set; }
    }
}
