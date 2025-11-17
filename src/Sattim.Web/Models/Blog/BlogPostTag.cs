using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Blog
{
    /// <summary>
    /// BlogPost ile Tag arasında çoka-çok ilişkiyi kuran bağlantı tablosu.
    /// </summary>
    public class BlogPostTag
    {
        #region Bileşik Anahtar (Composite Key) Özellikleri

        // Bu iki alan birlikte bu tablonun Birincil Anahtarını (PK) oluşturur.
        // Bu yapılandırma DbContext -> OnModelCreating içinde yapılmalıdır.

        [Required]
        public int BlogPostId { get; private set; }

        [Required]
        public int TagId { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: İlişkinin blog yazısı tarafı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("BlogPostId")]
        public virtual BlogPost BlogPost { get; private set; }

        /// <summary>
        /// Navigasyon: İlişkinin etiket tarafı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("TagId")]
        public virtual Tag Tag { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private BlogPostTag() { }

        /// <summary>
        /// Yeni bir 'BlogPostTag' ilişki nesnesi oluşturur ve ID'leri doğrular.
        /// </summary>
        public BlogPostTag(int blogPostId, int tagId)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID doğrulamaları eklendi.
            if (blogPostId <= 0)
                throw new ArgumentException("Geçersiz blog post kimliği.", nameof(blogPostId));
            if (tagId <= 0)
                throw new ArgumentException("Geçersiz etiket kimliği.", nameof(tagId));

            BlogPostId = blogPostId;
            TagId = tagId;
        }

        #endregion
    }
}