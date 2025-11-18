using Sattim.Web.ViewModels.Bid;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Bid
{
    public interface IBidService
    {
        // ====================================================================
        //  COMMANDS (Veri Yazma/Değiştirme İşlemleri)
        // ====================================================================

        /// <summary>
        /// Bir ürüne manuel (normal) bir teklif verir.
        /// İş Mantığı:
        /// 1. Product.Status == Active kontrolü yapar.
        /// 2. Product.EndDate kontrolü yapar.
        /// 3. Product.SellerId == userId kontrolü yapar (kendi ürünü mü?).
        /// 4. Gelen 'amount', (Product.CurrentPrice + Product.BidIncrement) kuralını geçiyor mu?
        /// 5. Product.UpdateCurrentPrice(amount) metodunu çağırır.
        /// 6. new Bid(...) constructor'ı ile yeni bir Bid kaydı oluşturur.
        /// 7. Gerekirse 'AutoBid' sistemini tetikler.
        /// 8. Değişiklikleri kaydeder.
        /// </summary>
        /// <param name="model">Teklif bilgilerini içeren ViewModel</param>
        /// <param name="userId">Teklifi veren kullanıcının ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, null)</returns>
        Task<(bool Success, string ErrorMessage)> PlaceBidAsync(PlaceBidViewModel model, string userId);

        /// <summary>
        /// Bir ürün için 'AutoBid' (Otomatik Teklif) ayarı oluşturur veya günceller.
        /// İş Mantığı:
        /// 1. (ProductId, UserId) kompozit anahtarı ile mevcut AutoBid kaydını arar.
        /// 2. Kayıt yoksa: new AutoBid(...) constructor'ı ile yenisini oluşturur.
        /// 3. Kayıt varsa: autoBid.UpdateSettings(...) metodunu çağırır.
        /// 4. Değişiklikleri kaydeder.
        /// </summary>
        /// <param name="model">Maksimum tutar ve artış miktarını içeren ViewModel</param>
        /// <param name="userId">Ayarı yapan kullanıcının ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, null)</returns>
        Task<(bool Success, string ErrorMessage)> PlaceAutoBidAsync(AutoBidViewModel model, string userId);

        /// <summary>
        /// Bir kullanıcının ürün için ayarlanmış Otomatik Teklif ayarını pasif hale getirir.
        /// İş Mantığı:
        /// 1. (ProductId, UserId) ile AutoBid kaydını bulur.
        /// 2. autoBid.Deactivate() metodunu çağırır.
        /// 3. Değişiklikleri kaydeder.
        /// </summary>
        /// <param name="productId">Ürün ID'si</param>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Başarı durumu</returns>
        Task<bool> CancelAutoBidAsync(int productId, string userId);


        // ====================================================================
        //  QUERIES (Veri Okuma İşlemleri)
        // ====================================================================

        /// <summary>
        /// Bir ürünün tüm teklif geçmişini (kim, ne zaman, ne kadar) getirir.
        /// ÖNEMLİ: Asla Product veya Bid domain modellerini döndürmez.
        /// </summary>
        /// <param name="productId">Ürün ID'si</param>
        /// <returns>Ürün detaylarını ve teklif listesini içeren bir ViewModel</returns>
        Task<ProductBidHistoryViewModel> GetProductBidHistoryAsync(int productId);

        /// <summary>
        /// Mevcut kullanıcının yaptığı tüm teklifleri, ihalelere göre gruplanmış
        /// ve filtrelenmiş olarak getirir. ("Tekliflerim" sayfası)
        /// </summary>
        /// <param name="userId">Mevcut kullanıcı ID'si</param>
        /// <param name="filter">Hangi tekliflerin (aktif, kazanılan, kaybedilen)
        /// listeleneceğini belirten filtre.</param>
        /// <returns>Kullanıcının teklif verdiği ihalelerin listesi</returns>
        Task<List<UserBidItemViewModel>> GetUserBidsAsync(string userId, BidFilterType filter);

        /// <summary>
        /// Otomatik Teklif ayarını (maksimum tutarını vb.) getirir.
        /// </summary>
        /// <param name="productId">Ürün ID'si</param>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Mevcut ayarı içeren ViewModel veya ayar yoksa null</returns>
        Task<AutoBidSettingViewModel> GetUserAutoBidSettingAsync(int productId, string userId);
    }

    /// <summary>
    /// GetUserBidsAsync metodu için filtreleme seçenekleri
    /// </summary>
    public enum BidFilterType
    {
        Active, // Devam eden ihaleler
        Won,    // Kazanılan ihaleler
        Lost,   // Kaybedilen ihaleler
        All     // Tümü
    }
}