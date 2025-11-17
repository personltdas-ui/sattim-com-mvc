using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Product
{
    /// <summary>
    // Bir kullanıcının bir ürüne yaptığı yorumu ve oylamayı temsil eder.
    /// Bir kullanıcı bir ürünü sadece bir kez oylayabilir (PK: ProductId, ReviewerId).
    /// </summary>
    public class ProductReview
    {
        #region Bileşik Anahtar (Composite Key) Özellikleri

        // Bu iki alan birlikte bu tablonun Birincil Anahtarını (PK) oluşturur.
        // Bu yapılandırma DbContext -> OnModelCreating içinde yapılmalıdır.

        /// <summary>
        /// Yorumun yapıldığı ürünün kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public int ProductId { get; private set; }

        /// <summary>
        /// Yorumu yapan kullanıcının kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public string ReviewerId { get; private set; }

        #endregion

        #region Diğer Özellikler (Properties)

        [StringLength(2000)]
        public string? Comment { get; private set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; private set; }

        public DateTime ReviewDate { get; private set; }
        public DateTime? LastModifiedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: Yorumun yapıldığı ürün.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product Product { get; private set; }

        /// <summary>
        /// Navigasyon: Yorumu yapan kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ReviewerId")]
        public virtual ApplicationUser Reviewer { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private ProductReview() { }

        /// <summary>
        /// Yeni bir 'ProductReview' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public ProductReview(int productId, string reviewerId, int rating, string? comment)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID doğrulamaları eklendi.
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(reviewerId))
                throw new ArgumentNullException(nameof(reviewerId), "Yorumu yapan kullanıcı kimliği boş olamaz.");

            // Alan (domain) kuralları ve atamalar için merkezi metodu kullan (DRY Prensibi)
            UpdateReview(rating, comment);

            ProductId = productId;
            ReviewerId = reviewerId;
            ReviewDate = DateTime.UtcNow;
            LastModifiedDate = null; // 'UpdateReview' bunu ayarlar, 'null'a geri çekiyoruz.
        }

        /// <summary>
        /// Yorumu günceller ve kuralları zorunlu kılar.
        /// </summary>
        public void UpdateReview(int newRating, string? newComment)
        {
            // DÜZELTME: Doğrulama CONSTRUCTOR'da da çalışsın diye buraya koyduk.
            if (newRating < 1 || newRating > 5)
                throw new ArgumentOutOfRangeException(nameof(newRating), "Puan 1-5 arası olmalıdır.");

            Rating = newRating;
            // İyileştirme: Boş string yerine 'null' kaydet
            Comment = string.IsNullOrWhiteSpace(newComment) ? null : newComment;
            LastModifiedDate = DateTime.UtcNow;
        }

        #endregion
    }
}