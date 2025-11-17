namespace Sattim.Web.ViewModels.DashBoard
{
    public class SalesItemViewModel
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; }
        public string? BuyerUserName { get; set; }
        public string? BuyerAvatarLetter { get; set; }
        public string? BuyerId { get; set; }
        public decimal SalePrice { get; set; }
        public decimal Commission { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime SaleDate { get; set; }

        
        public string StatusText { get; set; }
        public string StatusClass { get; set; }
        public string StatusIcon { get; set; }
    }
}
