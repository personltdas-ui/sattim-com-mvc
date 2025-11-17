using Sattim.Web.Models.Payment;
using Sattim.Web.Models.User;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Payment
{
    /// <summary>
    /// Iyzico, Stripe, PayPal gibi harici ödeme ağ geçitlerinin
    /// SDK'larıyla konuşan servisi soyutlar (abstracts).
    /// </summary>
    public interface IGatewayService
    {
        /// <summary>
        /// Bir ödeme kaydı ve kullanıcı bilgisi alıp, harici ağ
        /// geçidinde bir ödeme formu/oturumu oluşturur.
        /// </summary>
        /// <returns>Ağ geçidinin döndürdüğü HTML formu veya hata</returns>
        Task<(bool Success, string HtmlContent, string ErrorMessage)> CreateCheckoutFormAsync(
            Models.Payment.Payment payment,
            ApplicationUser user);
    }
}