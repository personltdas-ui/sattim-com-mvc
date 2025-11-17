using Sattim.Web.ViewModels.Dispute; // Gerekli ViewModel'lar
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Dispute
{
    
    public interface IDisputeService
    {
        /// <summary>
        /// Kullanıcının (Alıcı veya Satıcı) dahil olduğu tüm
        /// ihtilafları listeler ("İhtilaflarım" sayfası).
        /// </summary>
        /// <param name="userId">Giriş yapmış kullanıcı ID'si</param>
        /// <returns>İhtilaf özet listesi</returns>
        Task<List<DisputeSummaryViewModel>> GetMyDisputesAsync(string userId);

        /// <summary>
        /// Kullanıcının bir ihtilafın detaylarını ve mesajlarını görmesini sağlar.
        /// </summary>
        /// <param name="disputeId">İhtilaf ID'si</param>
        /// <param name="userId">Güvenlik kontrolü (bu kişi Alıcı veya Satıcı mı?)</param>
        /// <returns>İhtilafın tüm detayları</returns>
        Task<DisputeDetailViewModel> GetMyDisputeDetailsAsync(int disputeId, string userId);

        /// <summary>
        /// Alıcının, 'Funded' (Ödendi) veya 'Shipped' (Kargolandı)
        /// durumundaki bir sipariş ('Escrow') için yeni bir ihtilaf açmasını sağlar.
        /// İş Mantığı:
        /// 1. Transaction başlatır.
        /// 2. 'Escrow' ve 'Product'ı doğrular.
        /// 3. Güvenlik kontrolü (bu kişi Alıcı mı?).
        /// 4. Statü kontrolü (Escrow 'Funded' veya 'Shipped' mı?).
        /// 5. 'new Dispute(...)' constructor'ı ile ihtilafı oluşturur.
        /// 6. 'new DisputeMessage(...)' ile ilk mesajı (açıklama) ekler.
        /// 7. 'escrow.OpenDispute(...)' metodunu çağırır (Escrow statüsünü 'Disputed' yapar).
        /// 8. INotificationService ile satıcıyı ve adminleri bilgilendirir.
        /// 9. Transaction'ı onaylar.
        /// </summary>
        /// <param name="model">İhtilaf açma formu (ProductId, Sebep, Açıklama)</param>
        /// <param name="buyerId">İhtilafı açan alıcının ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, yeni disputeId)</returns>
        Task<(bool Success, int? DisputeId, string ErrorMessage)> OpenDisputeAsync(OpenDisputeViewModel model, string buyerId);

        /// <summary>
        /// Alıcının veya Satıcının mevcut bir ihtilafa yeni bir
        /// mesaj eklemesini sağlar.
        /// İş Mantığı:
        /// 1. 'Dispute'u bulur.
        /// 2. Güvenlik kontrolü (bu kişi Alıcı veya Satıcı mı?).
        /// 3. Statü kontrolü (Dispute 'Resolved'/'Closed' değil mi?).
        /// 4. 'new DisputeMessage(...)' oluşturur.
        /// 5. INotificationService ile karşı tarafı bilgilendirir.
        /// </summary>
        /// <param name="model">Mesaj ekleme formu (DisputeId, Message)</param>
        /// <param name="userId">Mesajı gönderen kullanıcı ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, null)</returns>
        Task<(bool Success, string ErrorMessage)> AddDisputeMessageAsync(AddDisputeMessageViewModel model, string userId);
    }
}