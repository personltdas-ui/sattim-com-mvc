namespace Sattim.Web.ViewModels.Order
{
    /// <summary>
    /// /Orders/Pay sayfasını doldurmak için kullanılan birleşik ViewModel.
    /// </summary>
    public class PaymentPageViewModel
    {
        public OrderDetailViewModel OrderDetails { get; set; }
        public OrderPaymentViewModel PaymentForm { get; set; }

        /// <summary>
        /// Kullanıcının cüzdanında ne kadar bakiye olduğunu göstermek için.
        /// </summary>
        public decimal AvailableWalletBalance { get; set; }
    }
}