using Sattim.Web.Models.Product;

namespace Sattim.Web.ViewModels.Product
{
    public class UserProductViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string PrimaryImageUrl { get; set; }
        public ProductStatus Status { get; set; }
        public DateTime EndDate { get; set; }
        public decimal StartingPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public int BidCount { get; set; }
    }
}