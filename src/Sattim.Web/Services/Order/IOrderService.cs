using Sattim.Web.Models.Bid;
using Sattim.Web.Models.Product;
using Sattim.Web.ViewModels.Order; // ViewModel'lar için
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Order
{
    public interface IOrderService
    {
        /// <summary>
        /// Süresi dolan bir ihaleyi sonuçlandırır.
        /// 'IAuctionJobService' tarafından tetiklenir.
        /// İş Mantığı:
        /// 1. Transaction başlatır.
        /// 2. Kazananı belirler (Rezerv fiyatı kontrol eder).
        /// 3. product.CloseAuction() metodunu çağırır.
        /// 4. Eğer SATILDIYSA:
        ///    a. new Escrow() oluşturur.
        ///    b. new Commission() oluşturur.
        ///    c. IProfileService'ten alıcının adresini alıp new ShippingInfo() oluşturur.
        ///    d. INotificationService'i tetikler (Kazanan, Kaybeden, Satıcı).
        /// 5. Eğer SATILMADIYSA:
        ///    a. Satıcıya INotificationService ile bilgi verir.
        /// 6. Transaction'ı tamamlar.
        /// </summary>
        /// <param name="product">İlgili Product nesnesi (Bids ve Seller dahil edilmeli)</param>
        /// <returns>Başarı durumu</returns>
        Task<bool> FinalizeAuctionAsync(Models.Product.Product product);

        /// <summary>
        /// Alıcının, kazandığı ve ödeme bekleyen/tamamlanan
        /// siparişlerini ("Siparişlerim") listeler.
        /// </summary>
        /// <param name="buyerId">Alıcının (mevcut kullanıcı) ID'si</param>
        /// <returns>Sipariş özet listesi</returns>
        Task<List<OrderSummaryViewModel>> GetMyOrdersAsync(string buyerId);

        /// <summary>
        /// Satıcının, sattığı ürünlerin sipariş durumunu ("Satışlarım") listeler.
        /// </summary>
        /// <param name="sellerId">Satıcının (mevcut kullanıcı) ID'si</param>
        /// <returns>Satış özet listesi</returns>
        Task<List<SalesSummaryViewModel>> GetMySalesAsync(string sellerId);

        /// <summary>
        /// Bir siparişin (Escrow) ödeme/kargo detaylarını
        /// hem alıcı hem de satıcı için gösterir.
        /// </summary>
        /// <param name="productId">Siparişin (Escrow) anahtarı olan Ürün ID'si</param>
        /// <param name="userId">Güvenlik kontrolü (bu kişi Alıcı veya Satıcı mı?)</param>
        /// <returns>Siparişin tüm detayları</returns>
        Task<OrderDetailViewModel> GetOrderDetailAsync(int productId, string userId);
    }
}