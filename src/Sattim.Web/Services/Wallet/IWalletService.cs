using Sattim.Web.ViewModels.Wallet; // Gerekli ViewModel'lar için
using System.Threading.Tasks;

namespace Sattim.Web.Services.Wallet
{
    public interface IWalletService
    {
        // ====================================================================
        //  1. KULLANICI İŞLEMLERİ (Commands & Queries)
        // ====================================================================

        /// <summary>
        /// Kullanıcının "Cüzdanım" sayfasını doldurur (Bakiye, Son İşlemler).
        /// </summary>
        /// <param name="userId">Giriş yapmış kullanıcı ID'si</param>
        /// <returns>Cüzdanın durumunu gösteren ViewModel</returns>
        Task<WalletDashboardViewModel> GetWalletDashboardAsync(string userId);

        /// <summary>
        /// Kullanıcının cüzdanından banka hesabına para çekmek için
        /// yeni bir talep ('PayoutRequest') oluşturur.
        /// İş Mantığı:
        /// 1. Transaction başlatır.
        /// 2. 'Wallet'ı bulur, 'wallet.Withdraw(amount)' çağırır (Bakiye düşer).
        /// 3. 'new PayoutRequest(...)' oluşturur (Statü: Pending).
        /// 4. 'new WalletTransaction(...)' oluşturur (Tip: Withdrawal, Tutar: -Amount).
        /// 5. Transaction'ı onaylar.
        /// </summary>
        /// <param name="model">Banka (IBAN) ve tutar bilgilerini içeren DTO</param>
        /// <param name="userId">Talebi oluşturan kullanıcı ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, null)</returns>
        Task<(bool Success, string ErrorMessage)> RequestPayoutAsync(PayoutRequestViewModel model, string userId);

        // ====================================================================
        //  2. SİSTEM & ADMİN İŞLEMLERİ (Commands)
        // ====================================================================

        /// <summary>
        /// (YENİ & KRİTİK) Parayı 'Escrow'dan satıcının 'Wallet'ına aktarır.
        /// Bu metot, (ileride yazılacak) 'IShippingService.MarkAsDelivered'
        /// veya 'IModerationService.ResolveDisputeForSeller' tarafından tetiklenir.
        /// İş Mantığı:
        /// 1. Transaction başlatır.
        /// 2. 'Escrow'u bulur, 'escrow.Release()' metodunu çağırır.
        /// 3. 'Commission'ı bulur, 'commission.MarkAsCollected()' çağırır.
        /// 4. Satıcının 'Wallet'ını bulur.
        /// 5. Net tutarı (Escrow.Amount - Commission.Amount) hesaplar.
        /// 6. 'wallet.Deposit(netAmount)' metodunu çağırır.
        /// 7. İki adet 'WalletTransaction' oluşturur:
        ///    a. (Satıcı için) Tip: Deposit, Tutar: +netAmount
        ///    b. (Sistem için) Tip: Commission, Tutar: +commissionAmount
        /// 8. Transaction'ı onaylar.
        /// </summary>
        /// <param name="escrowId">İşlemi tamamlanan 'Escrow' ID'si</param>
        /// <param name="adminOrSystemUserId">İşlemi tetikleyen (örn: "SYSTEM") ID'si</param>
        Task<(bool Success, string ErrorMessage)> ReleaseFundsToSellerAsync(int escrowId, string adminOrSystemUserId);

        /// <summary>
        /// (Admin) Para çekme talebini ('PayoutRequest') onaylar.
        /// </summary>
        /// <param name="payoutRequestId">Talep ID'si</param>
        /// <param name="adminId">Onaylayan adminin ID'si</param>
        Task<(bool Success, string ErrorMessage)> ApprovePayoutAsync(int payoutRequestId, string adminId);

        /// <summary>
        /// (Admin) Parayı bankaya *manuel* gönderdikten sonra talebi 'Tamamlandı'
        /// olarak işaretler.
        /// </summary>
        /// <param name="payoutRequestId">Talep ID'si</param>
        /// <param name="adminId">Tamamlayan adminin ID'si</param>
        Task<(bool Success, string ErrorMessage)> CompletePayoutAsync(int payoutRequestId, string adminId);

        /// <summary>
        /// (Admin) Para çekme talebini reddeder. Para cüzdana iade edilir.
        /// </summary>
        /// <param name="payoutRequestId">Talep ID'si</param>
        /// <param name="adminId">Reddeden adminin ID'si</param>
        /// <param name="reason">Reddetme sebebi</param>
        Task<(bool Success, string ErrorMessage)> RejectPayoutAsync(int payoutRequestId, string adminId, string reason);

        /// <summary>
        /// (Admin/Moderator) Bir ihtilaf (Dispute) alıcı lehine sonuçlandığında,
        /// 'Escrow'daki parayı alıcının 'Wallet'ına iade eder.
        /// İş Mantığı:
        /// 1. Transaction başlatır.
        /// 2. 'Escrow'u bulur, 'escrow.Refund()' metodunu çağırır.
        /// 3. Alıcının 'Wallet'ını bulur.
        /// 4. 'wallet.Deposit(escrow.Amount)' metodunu çağırır.
        /// 5. 'new WalletTransaction(...)' oluşturur (Tip: Refund, Tutar: +Amount).
        /// 6. Transaction'ı onaylar.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> RefundEscrowToBuyerAsync(int escrowId, string adminId, string reason);
    }
}