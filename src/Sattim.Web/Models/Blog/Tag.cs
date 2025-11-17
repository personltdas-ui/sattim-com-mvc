using Sattim.Web.Models.Blog;
using System; // ArgumentNullException için eklendi
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Index] için (alternatif olarak Fluent API)

namespace Sattim.Web.Models.Blog
{
    /// <summary>
    /// Blog yazılarını kategorize etmek için kullanılan bir etiketi temsil eder.
    /// </summary>
    // 'Slug' alanının benzersiz (unique) olması DbContext'te tanımlanmalıdır.
    // [Index(nameof(Slug), IsUnique = true)] // (EF Core 6.0 ve altı için)
    public class Tag
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(100)]
        public string Name { get; private set; }

        [Required]
        [StringLength(100)]
        public string Slug { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: Bu etiketi kullanan blog yazıları (Çoka-Çok bağlantı tablosu).
        /// DÜZELTME: 'IEnumerable<T>' -> 'ICollection<T>' olarak değiştirildi (EF Core için zorunlu).
        /// DÜZELTME: 'virtual' eklendi (Tembel Yükleme için).
        /// </summary>
        public virtual ICollection<BlogPostTag> BlogPostTags { get; private set; } = new List<BlogPostTag>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Tag() { }

        /// <summary>
        /// Yeni bir 'Tag' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public Tag(string name, string slug)
        {
            // Gerekli doğrulamalar 'Update' metoduna taşındı (DRY Prensibi)
            Update(name, slug);
        }

        /// <summary>
        /// Etiketin adını ve slug'ını günceller ve doğrulamaları uygular.
        /// </summary>
        public void Update(string name, string slug)
        {
            // DÜZELTME: Kapsüllemeyi korumak için doğrulamalar eklendi.
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Etiket adı boş olamaz.", nameof(name));
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Etiket slug (URL) boş olamaz.", nameof(slug));

            Name = name;
            Slug = slug;
        }

        #endregion
    }
}