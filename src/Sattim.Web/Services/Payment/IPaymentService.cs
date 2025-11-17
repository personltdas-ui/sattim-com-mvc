using Sattim.Web.ViewModels.Payment; // Gerekli ViewModel'lar için
using System.Threading.Tasks;

namespace Sattim.Web.Services.Payment
{
    public interface IPaymentService
    {
        /// <summary>
        /// Kredi kartı/banka kartı gibi harici bir ödeme ağ geçidi (Iyzico, Stripe vb.)
        /// kullanarak ödeme oturumu başlatır.
        /// İş Mantığı:
        /// 1. 'Escrow'u (güvenli hesap) bulur, tutarı ve alıcıyı doğrular.
        /// 2. Veritabanında 'Pending' (Beklemede) statüsünde bir 'Payment' kaydı oluşturur.
        /// 3. Ödeme ağ geçidine (Iyzico) bağlanıp bir ödeme formu/linki oluşturur.
        /// 4. Kullanıcıyı yönlendirmek için bu bilgileri döndürür.
        /// </summary>
        /// <param name="escrowId">Ödemesi yapılacak olan 'Escrow' kaydının ID'si (yani ProductId)</param>
        /// <param name="userId">Ödemeyi yapan alıcının ID'si</param>
        /// <returns>Kullanıcıyı ödeme sayfasına yönlendirecek bilgileri içeren ViewModel</returns>
        Task<CheckoutViewModel> CreateGatewayCheckoutAsync(int escrowId, string userId);

        /// <summary>
        /// Sitenin dahili cüzdanını kullanarak 'Escrow' için ödeme yapar.
        /// İş Mantığı:
        /// 1. Bir veritabanı 'transaction'ı başlatır.
        /// 2. 'Escrow'u bulur (Tutar: 100 TL).
        /// 3. 'Wallet'ı bulur, 'wallet.Withdraw(100)' metodunu çağırır (Bakiye kontrolü yapılır).
        /// 4. 'escrow.Fund()' metodunu çağırır (Escrow statüsü 'Funded' olur).
        /// 5. 'new Payment(...)' ile 'Completed' statüsünde bir 'Payment' kaydı oluşturur.
        /// 6. 'new WalletTransaction(...)' ile (Amount: -100 TL) cüzdan hareketini kaydeder.
        /// 7. 'Transaction'ı onaylar (Commit).
        /// </summary>
        /// <param name="escrowId">Ödemesi yapılacak 'Escrow' ID'si</param>
        /// <param name="userId">Ödemeyi yapan alıcının ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, null)</returns>
        Task<(bool Success, string ErrorMessage)> PayWithWalletAsync(int escrowId, string userId);

        /// <summary>
        /// Ödeme ağ geçidinden (Iyzico, Stripe vb.) gelen 'callback'
        /// veya 'webhook' bildirimini işler.
        /// İş Mantığı:
        /// 1. Bir 'transaction' başlatır.
        /// 2. Gelen 'paymentId' ile 'Pending' statüsündeki 'Payment' kaydını bulur.
        /// 3. Ödeme başarılıysa:
        ///    a. 'payment.Complete(...)' metodunu çağırır.
        ///    b. İlgili 'Escrow' kaydını bulur ve 'escrow.Fund()' metodunu çağırır.
        ///    c. 'INotificationService' ile alıcıyı/satıcıyı bilgilendirir.
        /// 4. Ödeme başarısızsa:
        ///    a. 'payment.Fail(...)' metodunu çağırır.
        /// 5. 'Transaction'ı onaylar (Commit).
        /// </summary>
        /// <param name="confirmation">Callback'ten gelen verileri içeren bir DTO</param>
        /// <returns>Ağ geçidine döndürülecek başarı durumu</returns>
        Task<bool> ProcessPaymentConfirmationAsync(PaymentConfirmationViewModel confirmation);
    }
}