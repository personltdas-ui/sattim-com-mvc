namespace Sattim.Web.ViewModels.Product
{
    public class ProductSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string PrimaryImageUrl { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime EndDate { get; set; }
        public int BidCount { get; set; }
    }
}