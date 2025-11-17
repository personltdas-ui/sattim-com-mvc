using Sattim.Web.Models.Product;
using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.User
{
    /// <summary>
    /// Bir kullanıcının başka bir kullanıcıyı belirli bir ürün
    /// işlemi üzerinden değerlendirmesini temsil eder (Çoka-Çok ilişki).
    /// </summary>
    public class UserRating
    {
        #region Bileşik Anahtar (Composite Key) Özellikleri

        // Bu üç alan birlikte bu tablonun Birincil Anahtarını (PK) oluşturur.
        // Bu yapılandırma DbContext -> OnModelCreating içinde yapılmalıdır.

        /// <summary>
        /// Değerlendirilen kullanıcının kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public string RatedUserId { get; private set; }

        /// <summary>
        /// Değerlendirmeyi yapan kullanıcının kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public string RaterUserId { get; private set; }

        /// <summary>
        /// Değerlendirmenin yapıldığı ürünün kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public int ProductId { get; private set; }

        #endregion

        #region Diğer Özellikler (Properties)

        [Required]
        [Range(1, 5)]
        public int Rating { get; private set; }

        [StringLength(500)]
        public string? Comment { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? LastModifiedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        // DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.

        [ForeignKey("RatedUserId")]
        public virtual ApplicationUser RatedUser { get; private set; }

        [ForeignKey("RaterUserId")]
        public virtual ApplicationUser RaterUser { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private UserRating() { }

        /// <summary>
        /// Yeni bir 'UserRating' nesnesi oluşturur ve alan doğrulamalarını yapar.
        /// </summary>
        public UserRating(int productId, string raterUserId, string ratedUserId, int rating, string? comment)
        {
            if (string.Equals(raterUserId, ratedUserId))
                throw new InvalidOperationException("Kullanıcı kendi kendini değerlendiremez.");

            if (rating < 1 || rating > 5)
                throw new ArgumentOutOfRangeException(nameof(rating), "Puan 1-5 arası olmalıdır.");

            if (productId <= 0)
                throw new ArgumentException("Geçersiz ProductId.", nameof(productId));

            if (string.IsNullOrWhiteSpace(raterUserId))
                throw new ArgumentException("Değerlendiren kullanıcı kimliği boş olamaz.", nameof(raterUserId));

            if (string.IsNullOrWhiteSpace(ratedUserId))
                throw new ArgumentException("Değerlendirilen kullanıcı kimliği boş olamaz.", nameof(ratedUserId));

            ProductId = productId;
            RaterUserId = raterUserId;
            RatedUserId = ratedUserId;
            Rating = rating;
            // İyileştirme: Boş string yerine 'null' kaydet
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = null;
        }

        /// <summary>
        /// Mevcut bir değerlendirmeyi günceller.
        /// </summary>
        public void Update(int newRating, string? newComment)
        {
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