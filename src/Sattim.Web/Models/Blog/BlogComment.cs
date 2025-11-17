using Sattim.Web.Models.User;
using Sattim.Web.Models.Blog;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Blog
{
    /// <summary>
    /// Bir blog yazısına yapılmış tek bir yorumu temsil eder.
    /// Yorumlar varsayılan olarak moderasyon (onay) gerektirir.
    /// </summary>
    public class BlogComment
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; private set; }

        /// <summary>
        /// Yorumun moderatör tarafından onaylanıp yayınlanmadığını belirtir.
        /// </summary>
        public bool IsApproved { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? LastModifiedDate { get; private set; } // Moderasyon/Güncelleme takibi

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Yorumun yapıldığı blog yazısının kimliği (Foreign Key).
        /// </summary>
        [Required]
        public int BlogPostId { get; private set; }

        /// <summary>
        /// Yorumu yapan kullanıcının kimliği (Foreign Key).
        /// </summary>
        [Required]
        public string UserId { get; private set; }

        /// <summary>
        /// Navigasyon: Yorumun yapıldığı blog yazısı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("BlogPostId")]
        public virtual BlogPost BlogPost { get; private set; }

        /// <summary>
        /// Navigasyon: Yorumu yapan kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private BlogComment() { }

        /// <summary>
        /// Yeni bir 'BlogComment' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public BlogComment(int blogPostId, string userId, string content)
        {
            // DÜZELTME: Kapsüllemeyi tamamlamak için ID doğrulaması eklendi.
            if (blogPostId <= 0)
                throw new ArgumentException("Geçersiz blog post kimliği.", nameof(blogPostId));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content), "Yorum içeriği boş olamaz.");

            BlogPostId = blogPostId;
            UserId = userId;
            Content = content;

            IsApproved = false; // Varsayılan: Moderasyon bekler
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = null;
        }

        /// <summary>
        /// Yorum içeriğini günceller ve onayı sıfırlar (yeniden moderasyon gerekir).
        /// </summary>
        public void UpdateContent(string newContent)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Yorum içeriği boş olamaz.", nameof(newContent));

            Content = newContent;
            IsApproved = false; // İçerik değişti, yeniden onay gerekebilir (Mükemmel alan mantığı)
            LastModifiedDate = DateTime.UtcNow;
        }

        // --- Moderasyon Metotları ---

        /// <summary>
        /// Yorumu onaylar ve görünür hale getirir.
        /// </summary>
        public void Approve()
        {
            if (IsApproved) return; // Zaten onaylıysa işlem yapma

            IsApproved = true;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Yorumun onayını kaldırır (veya reddeder).
        /// </summary>
        public void Reject() // Veya 'Unapprove'
        {
            if (!IsApproved) return; // Zaten onaylı değilse işlem yapma

            IsApproved = false;
            LastModifiedDate = DateTime.UtcNow;
        }

        #endregion
    }
}