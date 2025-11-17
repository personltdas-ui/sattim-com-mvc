using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Payment
{
    /// <summary>
    /// Harici bir ödeme ağ geçidinden (Iyzico/Stripe) dönen
    /// ödeme formunu/iframe'ini Controller'a (ve View'e) taşır.
    /// (CreateGatewayCheckoutAsync tarafından döndürülür)
    /// </summary>
    public class CheckoutViewModel
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Veritabanımızdaki Payment tablosunun ID'si.
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Ağ geçidinin (Iyzico) döndürdüğü HTML/script içeriği.
        /// </summary>
        public string HtmlContent { get; set; }
        public string GatewayName { get; set; } = "Iyzico"; // Örnek
    }

    /// <summary>
    /// Ödeme ağ geçidinden (Iyzico/Stripe) gelen 'callback'
    /// veya 'webhook' bildirimini temsil eder.
    /// (ProcessPaymentConfirmationAsync tarafından kullanılır)
    /// </summary>
    public class PaymentConfirmationViewModel
    {
        [Required]
        public int PaymentId { get; set; } // Bizim 'Pending' Payment kaydımızın ID'si

        [Required]
        public bool Success { get; set; }

        public string? TransactionId { get; set; } // Ağ geçidinin işlem ID'si
        public string? ErrorMessage { get; set; }
        public string? GatewayResponse { get; set; } // Loglama için tam JSON yanıtı
    }
}