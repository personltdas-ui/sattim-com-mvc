using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Product
{
    /// <summary>
    /// Bir ürüne (Product) ait tek bir görseli temsil eder.
    /// </summary>
    public class ProductImage
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(1000)] // URL için makul bir sınır
        public string ImageUrl { get; private set; }

        /// <summary>
        /// Bu resmin, ürünün ana (kapak) resmi olup olmadığını belirtir.
        /// (Bu mantık, 'Product' Aggregate Root'u tarafından yönetilmelidir).
        /// </summary>
        public bool IsPrimary { get; private set; }

        [Range(0, 100)]
        public int DisplayOrder { get; private set; }

        public DateTime UploadedDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Resmin ait olduğu ürünün kimliği (Foreign Key).
        /// </summary>
        [Required]
        public int ProductId { get; private set; }

        /// <summary>
        /// Navigasyon: Resmin ait olduğu ürün.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private ProductImage() { }

        /// <summary>
        /// Yeni bir 'ProductImage' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public ProductImage(int productId, string imageUrl, int displayOrder = 0)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID ve değerler doğrulandı.
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentNullException(nameof(imageUrl), "Resim URL'i boş olamaz.");

            ProductId = productId;
            ImageUrl = imageUrl;
            DisplayOrder = (displayOrder < 0) ? 0 : displayOrder; // Negatif olmamasını garanti et
            IsPrimary = false; // Varsayılan olarak hiçbir resim ana resim değildir
            UploadedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Bu resmi 'Ana Resim' olarak ayarlar (true/false).
        /// Not: Sadece bir resmin 'true' olmasını sağlama mantığı
        /// 'Product' sınıfı veya 'ProductService' içinde yönetilmelidir.
        /// </summary>
        public void SetAsPrimary(bool isPrimary)
        {
            IsPrimary = isPrimary;
        }

        /// <summary>
        /// Resmin görüntülenme sırasını günceller.
        /// </summary>
        public void UpdateDisplayOrder(int newOrder)
        {
            if (newOrder < 0)
                throw new ArgumentOutOfRangeException(nameof(newOrder), "Sıralama 0'dan küçük olamaz.");

            DisplayOrder = newOrder;
        }

        #endregion
    }
}