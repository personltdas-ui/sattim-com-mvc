using Sattim.Web.Models.User;
using Sattim.Web.Models.Blog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Blog
{
    public class BlogPost
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(255)]
        public string Title { get; private set; }

        [Required]
        [StringLength(300)]
        public string Slug { get; private set; }

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

        [StringLength(255)]
        public string? MetaTitle { get; private set; }
        [StringLength(500)]
        public string? MetaDescription { get; private set; }
        [StringLength(255)]
        public string? MetaKeywords { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public string AuthorId { get; private set; }

        [ForeignKey("AuthorId")]
        public virtual ApplicationUser Author { get; private set; }

        public virtual ICollection<BlogComment> Comments { get; private set; } = new List<BlogComment>();

        public virtual ICollection<BlogPostTag> BlogPostTags { get; private set; } = new List<BlogPostTag>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private BlogPost() { }

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

            Status = BlogPostStatus.Draft;
            ViewCount = 0;
            CreatedDate = DateTime.UtcNow;
        }

        public void Update(string title, string slug, string content, string? excerpt)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Başlık boş olamaz.", nameof(title));
            if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug (URL) boş olamaz.", nameof(slug));
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("İçerik boş olamaz.", nameof(content));

            Title = title;
            Slug = slug;
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

        public void Publish()
        {
            if (Status == BlogPostStatus.Published) return;

            Status = BlogPostStatus.Published;
            if (!PublishedDate.HasValue)
            {
                PublishedDate = DateTime.UtcNow;
            }
            ModifiedDate = DateTime.UtcNow;
        }

        public void Archive()
        {
            if (Status == BlogPostStatus.Archived) return;

            Status = BlogPostStatus.Archived;
            ModifiedDate = DateTime.UtcNow;
        }

        public void SetAsDraft()
        {
            if (Status == BlogPostStatus.Draft) return;

            Status = BlogPostStatus.Draft;
            PublishedDate = null;
            ModifiedDate = DateTime.UtcNow;
        }

        public void IncrementViewCount()
        {
            ViewCount++;
        }

        #endregion
    }

    public enum BlogPostStatus
    {
        Draft,
        Published,
        Archived
    }
}