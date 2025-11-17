using Sattim.Web.Models.Product;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Product varlığı için jenerik repository'ye EK OLARAK
    /// karmaşık sorgu (Include vb.) metotları sağlar.
    /// </summary>
    public interface IProductRepository : IGenericRepository<Models.Product.Product>
    {
        /// <summary>
        /// Bir ürünü, ilişkili tüm alt varlıklarıyla (Kategori, Satıcı, Resimler,
        /// Teklifler) birlikte yükler.
        /// </summary>
        Task<Models.Product.Product?> GetProductWithDetailsAsync(int productId);

        /// <summary>
        /// Ana sayfada gösterilecek aktif ve popüler ürünleri listeler.
        /// (Bu metot sayfalama (pagination) da içermelidir)
        /// </summary>
        Task<IEnumerable<Models.Product.Product>> GetHomepageProductsAsync(int count, int page);

        /// <summary>
        /// Bir kullanıcının sattığı tüm ürünleri listeler.
        /// </summary>
        Task<IEnumerable<Models.Product.Product>> GetProductsBySellerAsync(string sellerId);
        Task<List<Models.Product.Product>> GetPendingProductsForAdminAsync();

        /// <summary>
        /// (Refactor edildi) Bir satıcının ürünlerini statüye göre filtreleyerek getirir.
        /// </summary>
        Task<List<Models.Product.Product>> GetMyProductsAsync(string sellerId, ProductStatus? filter);

        /// <summary>
        /// "Ürünü Düzenle" formu için bir ürünü (ve resimlerini) getirir.
        /// </summary>
        Task<Models.Product.Product?> GetProductForEditAsync(int productId, string userId);

        /// <summary>
        /// Arama sorgusuna uyan ürünleri getirir.
        /// </summary>
        Task<List<Models.Product.Product>> SearchProductsAsync(string query);

        /// <summary>
        /// Ana katalog/arama metodu. Dinamik filtreleme ve sıralama yapar.
        /// </summary>
        Task<(List<Models.Product.Product> Products, int TotalCount)> GetProductsByFilterAsync(ProductFilterViewModel filter);
    }
}
