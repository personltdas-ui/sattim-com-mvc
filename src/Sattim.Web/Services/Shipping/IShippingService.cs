using Sattim.Web.ViewModels.Shipping; // Gerekli ViewModel'lar
using System.Threading.Tasks;

namespace Sattim.Web.Services.Shipping
{
    public interface IShippingService
    {
        /// <summary>
        /// (Satıcı) Bir siparişi "Kargolandı" olarak işaretler
        /// ve kargo bilgilerini ('ShippingInfo') günceller.
        /// İş Mantığı:
        /// 1. 'ShippingInfo' kaydını bulur (PK=ProductId).
        /// 2. Güvenlik kontrolü yapar (İşlemi yapan 'sellerId' mı?).
        /// 3. İlgili 'Escrow' kaydının 'Funded' (Ödendi) olduğunu doğrular.
        /// 4. 'shippingInfo.Ship(carrier, trackingNumber)' metodunu çağırır.
        /// 5. Değişiklikleri kaydeder.
        /// 6. 'INotificationService.SendProductShippedNotificationAsync' metodunu tetikler.
        /// </summary>
        /// <param name="model">Kargo firması ve takip no bilgilerini içerir</param>
        /// <param name="sellerId">İşlemi yapan satıcının ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, null)</returns>
        Task<(bool Success, string ErrorMessage)> MarkAsShippedAsync(MarkAsShippedViewModel model, string sellerId);

        /// <summary>
        /// (Alıcı) Bir siparişi "Teslim Edildi" olarak işaretler.
        /// BU, SİSTEMDEKİ PARANIN HAREKET ETMESİNİ TETİKLEYEN EN KRİTİK METOTTUR.
        /// İş Mantığı:
        /// 1. 'ShippingInfo' kaydını bulur.
        /// 2. Güvenlik kontrolü yapar (İşlemi yapan 'buyerId' mı?).
        /// 3. 'shippingInfo.Deliver()' metodunu çağırır.
        /// 4. **'IWalletService.ReleaseFundsToSellerAsync(productId, "SYSTEM")'** metodunu tetikler.
        /// 5. 'INotificationService.SendProductDeliveredNotificationAsync' metodunu tetikler.
        /// 6. Değişiklikleri kaydeder.
        /// </summary>
        /// <param name="productId">Siparişin (ShippingInfo) ID'si</param>
        /// <param name="buyerId">İşlemi yapan alıcının ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, null)</returns>
        Task<(bool Success, string ErrorMessage)> MarkAsDeliveredAsync(int productId, string buyerId);

        /// <summary>
        /// (Alıcı/Satıcı) Bir siparişin kargo durumunu ve bilgilerini getirir.
        /// </summary>
        /// <param name="productId">Siparişin ID'si</param>
        /// <param name="userId">Güvenlik kontrolü (Alıcı veya Satıcı mı?)</param>
        /// <returns>Kargo detaylarını içeren DTO</returns>
        Task<ShippingDetailViewModel> GetShippingDetailsAsync(int productId, string userId);
    }
}