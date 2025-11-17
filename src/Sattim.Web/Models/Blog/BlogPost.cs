using Sattim.Web.Models.User;
using Sattim.Web.Models.Blog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Blog
{
    /// <summary>
    /// Bir blog yazısını ve ilgili durumlarını/içeriğini temsil eder.
    /// </summary>
    public class BlogPost
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(255)]
        public string Title { get; private set; }

        [Required]
        [StringLength(300)]
        public string Slug { get; private set; } // Benzersizliği Service katmanında kontrol edilmeli

        [Required]
        public string Content { get; private set; }

        [StringLength(500)]
        public string? Excerpt { get; private set; }

        [StringLength(1000)]
        public string? FeaturedImage { get; private set; }

        [Required]
        public BlogPostStatus Status { get; private set; }

        [Range(0, int.MaxValue)]
        public int ViewCount { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? PublishedDate { get; private set; }
        public DateTime? ModifiedDate { get; private set; }

        // --- SEO Alanları ---
        [StringLength(255)]
        public string? MetaTitle { get; private set; }
        [StringLength(500)]
        public string? MetaDescription { get; private set; }
        [StringLength(255)]
        public string? MetaKeywords { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Yazının yazarının kimliği (Foreign Key).
        /// </summary>
        [Required]
        public string AuthorId { get; private set; }

        /// <summary>
        /// Navigasyon: Yazar (ApplicationUser).
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("AuthorId")]
        public virtual ApplicationUser Author { get; private set; }

        /// <summary>
        /// Navigasyon: Bu yazıya yapılan yorumlar (1'e Çok).
        /// DÜZELTME: 'IEnumerable<T>' -> 'ICollection<T>' olarak değiştirildi.
        /// DÜZELTME: 'virtual' eklendi.
        /// </summary>
        public virtual ICollection<BlogComment> Comments { get; private set; } = new List<BlogComment>();

        /// <summary>
        /// Navigasyon: Bu yazının etiketleri (Çoka Çok ilişki).
        /// DÜZELTME: 'IEnumerable<T>' -> 'ICollection<T>' olarak değiştirildi.
        /// DÜZELTME: 'virtual' eklendi.
        /// </summary>
        public virtual ICollection<BlogPostTag> BlogPostTags { get; private set; } = new List<BlogPostTag>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private BlogPost() { }

        /// <summary>
        /// Yeni bir 'BlogPost' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public BlogPost(string title, string slug, string content, string authorId, string? excerpt = null)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));
            if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentNullException(nameof(slug));
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrWhiteSpace(authorId)) throw new ArgumentNullException(nameof(authorId));

            Title = title;
            Slug = slug;
            Content = content;
            AuthorId = authorId;
            Excerpt = excerpt;

            Status = BlogPostStatus.Draft; // Her zaman 'Taslak' olarak başlar
            ViewCount = 0;
            CreatedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Yazının ana içeriğini günceller ve doğrulamaları uygular.
        /// </summary>
        public void Update(string title, string slug, string content, string? excerpt)
        {
            // DÜZELTME: Kapsüllemeyi korumak için doğrulamalar eklendi.
            // Nesnenin geçersiz bir duruma (örn. boş başlık) güncellenmesini engeller.
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Başlık boş olamaz.", nameof(title));
            if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug (URL) boş olamaz.", nameof(slug));
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("İçerik boş olamaz.", nameof(content));

            Title = title;
            Slug = slug; // Servis katmanı, slug'ın benzersizliğini ayrıca kontrol etmeli
            Content = content;
            Excerpt = excerpt;
            ModifiedDate = DateTime.UtcNow;
        }

        public void UpdateSeo(string? metaTitle, string? metaDescription, string? metaKeywords)
        {
            MetaTitle = metaTitle;
            MetaDescription = metaDescription;
            MetaKeywords = metaKeywords;
            ModifiedDate = DateTime.UtcNow;
        }

        public void UpdateFeaturedImage(string? imageUrl)
        {
            FeaturedImage = imageUrl;
            ModifiedDate = DateTime.UtcNow;
        }

        // --- Durum Makinesi Metotları (State Machine Methods) ---

        public void Publish()
        {
            if (Status == BlogPostStatus.Published) return; // Zaten yayınlanmış

            Status = BlogPostStatus.Published;
            // Sadece ilk yayınlanmada tarihi ayarla
            if (!PublishedDate.HasValue)
            {
                PublishedDate = DateTime.UtcNow;
            }
            ModifiedDate = DateTime.UtcNow;
        }

        public void Archive()
        {
            if (Status == BlogPostStatus.Archived) return; // Zaten arşivlenmiş

            Status = BlogPostStatus.Archived;
            ModifiedDate = DateTime.UtcNow;
        }

        public void SetAsDraft()
        {
            if (Status == BlogPostStatus.Draft) return; // Zaten taslak

            Status = BlogPostStatus.Draft;
            PublishedDate = null; // Taslağa geri dönerse yayın tarihi kalkar (İyi bir karar)
            ModifiedDate = DateTime.UtcNow;
        }

        public void IncrementViewCount()
        {
            ViewCount++;
            // Not: Bu işlem için 'ModifiedDate' güncellenmez.
        }

        #endregion
    }

    /// <summary>
    /// Bir blog yazısının durumunu belirler (Taslak, Yayınlanmış, Arşivlenmiş).
    /// </summary>
    public enum BlogPostStatus
    {
        Draft,
        Published,
        Archived
    }
}