using Microsoft.AspNetCore.Http;
using Sattim.Web.Models.Product; // ProductStatus enum'u için
using Sattim.Web.ViewModels; // Gerekli ViewModel
using Sattim.Web.ViewModels.Category;
using Sattim.Web.ViewModels.Product; // Gerekli ViewModel
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Product
{
    public interface IProductService
    {
        // ====================================================================
        //  1. PUBLIC QUERIES (Genel Katalog/Arama İşlemleri)
        // ====================================================================

        /// <summary>
        /// Filtrelenmiş, sayfalanmış ve sıralanmış ürün listesini getirir.
        /// (Ana sayfa, kategori sayfası vb. için)
        /// </summary>
        /// <param name="filter">Tüm filtreleme, sıralama ve sayfalama
        /// bilgilerini içeren bir DTO</param>
        /// <returns>Ürün listesi ve toplam sayfa sayısı</returns>
        Task<(List<ProductSummaryViewModel> Products, int TotalPages)> GetProductListAsync(ProductFilterViewModel filter);

        /// <summary>
        /// Bir ürünün detay sayfasını getirir.
        /// ÖNEMLİ: Bu metot, 'ProductView' ve 'ProductAnalytics'
        /// modellerini de güncellemelidir (loglama).
        /// </summary>
        /// <param name="productId">Ürün ID'si</param>
        /// <param name="userId">Görüntüleyen kullanıcı (eğer giriş yapmışsa)</param>
        /// <param name="ipAddress">Görüntüleyenin IP adresi</param>
        /// <returns>Ürün detaylarını içeren ViewModel</returns>
        Task<ProductDetailViewModel> GetProductDetailsAsync(int productId, string? userId, string ipAddress);

        /// <summary>
        /// Arama sorgusuna uyan ürünleri getirir.
        /// ÖNEMLİ: Bu metot, 'SearchHistory' ve 'PopularSearch'
        /// modellerini de güncellemelidir.
        /// </summary>
        /// <param name="query">Arama terimi</param>
        /// <param name="userId">Arayan kullanıcı (eğer giriş yapmışsa)</param>
        /// <param name="ipAddress">Arayanın IP adresi</param>
        /// <returns>Arama sonuçları ve toplam sayı</returns>
        Task<(List<ProductSummaryViewModel> Products, int ResultCount)> GetSearchResultsAsync(string query, string? userId, string ipAddress);

        /// <summary>
        /// (Refactor edildi) Kategori listesini DTO olarak getirir.
        /// </summary>
        /// <returns>Kategori ağacını (hiyerarşi) içeren ViewModel listesi</returns>
        Task<List<CategoryViewModel>> GetCategoriesAsync();

        // ====================================================================
        //  2. USER COMMANDS (Kullanıcı/Satıcı Ürün Yönetimi)
        // ====================================================================

        /// <summary>
        /// "Yeni Ürün Yarat" formunu doldurmak için gerekli
        /// verileri (Kategoriler vb.) getirir.
        /// </summary>
        Task<ProductFormViewModel> GetProductForCreateAsync();

        /// <summary>
        /// (Refactor edildi) Yeni bir ürünü 'Pending' (Onay Bekliyor)
        /// statüsünde oluşturur.
        /// İş Mantığı: new Product(...) constructor'ını çağırır.
        /// </summary>
        /// <param name="model">Formdan gelen veriler</param>
        /// <param name="userId">Satıcı ID'si</param>
        /// <returns>Başarı durumu, (varsa) hata mesajı ve yaratılan ürünün ID'si</returns>
        Task<(bool Success, int? ProductId, string ErrorMessage)> CreateProductAsync(ProductFormViewModel model, string userId);

        /// <summary>
        /// "Ürünü Düzenle" formunu doldurmak için mevcut
        /// ürünün verilerini getirir.
        /// </summary>
        /// <param name="productId">Ürün ID'si</param>
        /// <param name="userId">Güvenlik kontrolü (ürünün sahibi mi?)</param>
        Task<ProductFormViewModel> GetProductForEditAsync(int productId, string userId);

        /// <summary>
        /// (Refactor edildi) Mevcut bir ürünü günceller.
        /// İş Mantığı: product.UpdateDetails(...) metodunu çağırır.
        /// </summary>
        /// <param name="productId">Ürün ID'si</param>
        /// <param name="model">Formdan gelen veriler</param>
        /// <param name="userId">Güvenlik kontrolü (ürünün sahibi mi?)</param>
        Task<(bool Success, string ErrorMessage)> UpdateProductAsync(int productId, ProductFormViewModel model, string userId);

        /// <summary>
        /// (YENİ) Satıcının kendi aktif ürününü iptal etmesini sağlar.
        /// İş Mantığı: product.Cancel() metodunu çağırır.
        /// </summary>
        /// <param name="productId">Ürün ID'si</param>
        /// <param name="userId">Güvenlik kontrolü (ürünün sahibi mi?)</param>
        Task<(bool Success, string ErrorMessage)> CancelProductAsync(int productId, string userId);

        // ====================================================================
        //  3. USER QUERIES (Kullanıcı/Satıcı Panelim)
        // ====================================================================

        /// <summary>
        /// (Refactor edildi) Bir kullanıcının "Ürünlerim" sayfasını doldurur.
        /// </summary>
        /// <param name="userId">Satıcı ID'si</param>
        /// <param name="filter">Statüye göre filtre (Aktif, Satıldı, Beklemede)</param>
        /// <returns>Ürün listesi DTO'su</returns>
        Task<List<UserProductViewModel>> GetMyProductsAsync(string userId, ProductStatus? filter);

        // ====================================================================
        //  4. ADMIN/MODERATION COMMANDS (Yönetim Paneli)
        // ====================================================================

        /// <summary>
        /// (YENİ) Bir ürünü onaylar ve 'Active' statüsüne geçirir.
        /// İş Mantığı: product.Approve() metodunu çağırır.
        /// </summary>
        /// <param name="productId">Ürün ID'si</param>
        /// <param name="adminId">İşlemi yapan adminin ID'si</param>
        Task<bool> ApproveProductAsync(int productId, string adminId);

        /// <summary>
        /// (YENİ) Bir ürünü reddeder ('Cancelled' statüsüne geçirir)
        /// ve loglama yapar.
        /// </summary>
        /// <param name="productId">Ürün ID'si</param>
        /// <param name="adminId">İşlemi yapan adminin ID'si</param>
        /// <param name="reason">Reddetme sebebi (AuditLog'a yazılır)</param>
        Task<bool> RejectProductAsync(int productId, string adminId, string reason);

        /// <summary>
        /// (Refactor edildi) Bir ürünü (örn: spam)
        /// veritabanından tamamen siler.
        /// </summary>
        Task<bool> DeleteProductAsAdminAsync(int productId, string adminId);

        // ====================================================================
        //  5. IMAGE MANAGEMENT (Resim Yönetimi)
        // ====================================================================

        /// <summary>
        /// (YENİ) Bir ürüne yeni resimler ekler.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> AddProductImagesAsync(int productId, List<IFormFile> images, string userId);

        /// <summary>
        /// (YENİ) Bir üründen tek bir resmi siler.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> DeleteProductImageAsync(int imageId, string userId);

        /// <summary>
        /// (YENİ) Bir ürünün resimlerinin sırasını veya
        /// hangisinin ana resim (IsPrimary) olduğunu günceller.
        /// </summary>
        Task<bool> UpdateImageOrderAsync(int productId, List<ImageOrderViewModel> imageOrders, string userId);
    }
}