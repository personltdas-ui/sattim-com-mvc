namespace Sattim.Web.ViewModels.Escrow
{
    public class EscrowViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string BuyerName { get; set; }
        public string SellerName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ReleasedDate { get; set; }
        public string? DisputeReason { get; set; }
    }
}
