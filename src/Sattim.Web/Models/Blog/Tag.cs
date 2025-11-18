using Sattim.Web.Models.Blog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Blog
{
    public class Tag
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(100)]
        public string Name { get; private set; }

        [Required]
        [StringLength(100)]
        public string Slug { get; private set; }

        #endregion

        #region Navigasyon Özellikleri

        public virtual ICollection<BlogPostTag> BlogPostTags { get; private set; } = new List<BlogPostTag>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private Tag() { }

        public Tag(string name, string slug)
        {
            Update(name, slug);
        }

        public void Update(string name, string slug)
        {
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