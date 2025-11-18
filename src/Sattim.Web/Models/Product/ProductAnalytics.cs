using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Product
{
    public class ProductAnalytics
    {
        #region Özellikler ve Bire-Bir İlişki

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

        #region Navigasyon Özellikleri

        public virtual Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private ProductAnalytics() { }

        public ProductAnalytics(int productId)
        {
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