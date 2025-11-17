namespace Sattim.Web.ViewModels.Product
{
    public class ProductSellerViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string ProfileImageUrl { get; set; }
        public decimal AverageRating { get; set; } // (UserProfile'dan)
        public int RatingCount { get; set; } // (UserProfile'dan)
    }
}
