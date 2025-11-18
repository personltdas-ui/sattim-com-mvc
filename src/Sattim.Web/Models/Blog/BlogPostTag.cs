using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace Sattim.Web.Models.Blog
{
    public class BlogPostTag
    {
        #region Bileşik Anahtar Özellikleri

        [Required]
        public int BlogPostId { get; private set; }

        [Required]
        public int TagId { get; private set; }

        #endregion

        #region Navigasyon Özellikleri

        [ForeignKey("BlogPostId")]
        public virtual BlogPost BlogPost { get; private set; }

        [ForeignKey("TagId")]
        public virtual Tag Tag { get; private set; }

        #endregion

        #region Yapıcı Metotlar

        private BlogPostTag() { }

        public BlogPostTag(int blogPostId, int tagId)
        {
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