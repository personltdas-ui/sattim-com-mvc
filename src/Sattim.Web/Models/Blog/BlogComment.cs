using Sattim.Web.Models.User;
using Sattim.Web.Models.Blog;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Blog
{
    public class BlogComment
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; private set; }

        public bool IsApproved { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? LastModifiedDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public int BlogPostId { get; private set; }

        [Required]
        public string UserId { get; private set; }

        [ForeignKey("BlogPostId")]
        public virtual BlogPost BlogPost { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private BlogComment() { }

        public BlogComment(int blogPostId, string userId, string content)
        {
            if (blogPostId <= 0)
                throw new ArgumentException("Geçersiz blog post kimliği.", nameof(blogPostId));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content), "Yorum içeriği boş olamaz.");

            BlogPostId = blogPostId;
            UserId = userId;
            Content = content;

            IsApproved = false;
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = null;
        }

        public void UpdateContent(string newContent)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Yorum içeriği boş olamaz.", nameof(newContent));

            Content = newContent;
            IsApproved = false;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Approve()
        {
            if (IsApproved) return;

            IsApproved = true;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Reject()
        {
            if (!IsApproved) return;

            IsApproved = false;
            LastModifiedDate = DateTime.UtcNow;
        }

        #endregion
    }
}