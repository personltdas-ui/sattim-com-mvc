using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Key], [ForeignKey] için eklendi

namespace Sattim.Web.Models.Product
{
    /// <summary>
    /// Bir ürüne (Product) ait analitik verileri (sayaçları) temsil eder.
    /// Performans için ana 'Product' tablosundan ayrılmıştır.
    /// Product ile Bire-Bir (1-to-1) ilişkiye sahiptir.
    /// </summary>
    public class ProductAnalytics
    {
        #region Özellikler ve Bire-Bir İlişki

        /// <summary>
        /// Bu tablonun Birincil Anahtarı (PK).
        /// Aynı zamanda Product tablosuna olan Yabancı Anahtardır (FK).
        /// Bu, birebir ilişkiyi garanti eder.
        /// </summary>
        [Key]
        [ForeignKey("Product")]
        public int ProductId { get; private set; }

        [Range(0, int.MaxValue)]
        public int ViewCount { get; private set; }

        [Range(0, int.MaxValue)]
        public int UniqueViewCount { get; private set; }

        [Range(0, int.MaxValue)]
        public int FavoriteCount { get; private set; }

        [Range(0, int.MaxValue)]
        public int WatchListCount { get; private set; }

        [Range(0, int.MaxValue)]
        public int ShareCount { get; private set; }

        public DateTime LastUpdated { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: Analitiğin ait olduğu ürün.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        public virtual Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private ProductAnalytics() { }

        /// <summary>
        /// Yeni bir 'ProductAnalytics' nesnesi oluşturur ve tüm sayaçları sıfırlar.
        /// </summary>
        public ProductAnalytics(int productId)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID doğrulaması eklendi.
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));

            ProductId = productId;
            ViewCount = 0;
            UniqueViewCount = 0;
            FavoriteCount = 0;
            WatchListCount = 0;
            ShareCount = 0;
            UpdateTimestamp();
        }

        // --- Artırmalı Sayaçlar (Incremental Counters) ---

        public void IncrementView()
        {
            ViewCount++;
            UpdateTimestamp();
        }

        public void IncrementUniqueView()
        {
            UniqueViewCount++;
            UpdateTimestamp();
        }

        public void IncrementShare()
        {
            ShareCount++;
            UpdateTimestamp();
        }

        // --- Yeniden Hesaplanan Sayaçlar (Re-calculated Counters) ---
        // (Bu metotlar genellikle bir 'Background Service' veya 'EventHandler' tarafından çağrılır)

        public void UpdateFavoriteCount(int newCount)
        {
            if (newCount < 0)
                throw new ArgumentOutOfRangeException(nameof(newCount), "Sayı 0'dan küçük olamaz.");

            FavoriteCount = newCount;
            UpdateTimestamp();
        }

        public void UpdateWatchListCount(int newCount)
        {
            if (newCount < 0)
                throw new ArgumentOutOfRangeException(nameof(newCount), "Sayı 0'dan küçük olamaz.");

            WatchListCount = newCount;
            UpdateTimestamp();
        }

        private void UpdateTimestamp()
        {
            LastUpdated = DateTime.UtcNow;
        }

        #endregion
    }
}