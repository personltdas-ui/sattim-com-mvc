using Sattim.Web.Models.Product;
using System; // ArgumentNullException vb. için eklendi
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Category
{
    /// <summary>
    /// Ürünleri gruplamak için kullanılan, kendi kendine hiyerarşi
    /// kurabilen (alt kategoriler) bir varlığı temsil eder.
    /// </summary>
    public class Category
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(100)]
        public string Name { get; private set; }

        [Required]
        [StringLength(100)]
        public string Slug { get; private set; } // Benzersizliği DbContext'te sağlanmalı

        [StringLength(500)]
        public string? Description { get; private set; }

        [StringLength(1000)]
        public string? ImageUrl { get; private set; }

        public bool IsActive { get; private set; }

        #endregion

        #region İlişkiler ve Hiyerarşi (Relationships & Hierarchy)

        /// <summary>
        /// Üst kategorinin kimliği (Foreign Key). 
        /// Ana kategoriler için 'null' olabilir.
        /// </summary>
        public int? ParentCategoryId { get; private set; }

        /// <summary>
        /// Navigasyon: Üst Kategori.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ParentCategoryId")]
        public virtual Category? ParentCategory { get; private set; }

        /// <summary>
        /// Navigasyon: Bu kategorinin altındaki kategoriler (1'e Çok).
        /// DÜZELTME: 'IEnumerable<T>' -> 'ICollection<T>' ve 'virtual' eklendi.
        /// </summary>
        public virtual ICollection<Category> SubCategories { get; private set; } = new List<Category>();

        /// <summary>
        /// Navigasyon: Bu kategorideki ürünler (1'e Çok).
        /// DÜZELTME: 'IEnumerable<T>' -> 'ICollection<T>' ve 'virtual' eklendi.
        /// </summary>
        public virtual ICollection<Product.Product> Products { get; private set; } = new List<Product.Product>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Category() { }

        /// <summary>
        /// Yeni bir 'Category' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public Category(string name, string slug, int? parentCategoryId = null, string? description = null, string? imageUrl = null)
        {
            // Gerekli doğrulamalar metotlara taşındı (DRY Prensibi)
            UpdateDetails(name, slug, description);
            ChangeParent(parentCategoryId);
            UpdateImageUrl(imageUrl);

            IsActive = true; // Varsayılan olarak aktif başla
        }

        /// <summary>
        /// Kategori detaylarını günceller ve doğrulamaları uygular.
        /// </summary>
        public void UpdateDetails(string name, string slug, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Kategori adı boş olamaz.", nameof(name));
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Kategori slug (URL) boş olamaz.", nameof(slug));

            Name = name;
            Slug = slug;
            Description = description;
        }

        public void UpdateImageUrl(string? newUrl)
        {
            ImageUrl = newUrl;
        }

        /// <summary>
        /// Kategorinin ebeveynini değiştirir ve ID'yi doğrular.
        /// </summary>
        public void ChangeParent(int? newParentCategoryId)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID doğrulaması eklendi.
            if (newParentCategoryId.HasValue && newParentCategoryId.Value <= 0)
                throw new ArgumentException("Geçersiz ebeveyn kategori kimliği.", nameof(newParentCategoryId));

            // Not: Döngüsel referans (bir kategoriyi kendi alt kategorisi yapmak)
            // kontrolü, bu nesnenin bilgisi dışında olduğundan
            // 'Service' katmanında yapılmalıdır. (Yorumunuz doğruydu)
            ParentCategoryId = newParentCategoryId;
        }

        // --- Durum Metotları ---

        public void Deactivate()
        {
            if (!IsActive) return; // Zaten pasifse işlem yapma
            IsActive = false;
        }

        public void Activate()
        {
            if (IsActive) return; // Zaten aktifse işlem yapma
            IsActive = true;
        }

        #endregion
    }
}