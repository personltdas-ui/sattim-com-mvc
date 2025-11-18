using Sattim.Web.Models.Product;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Category
{
    public class Category
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

        [StringLength(500)]
        public string? Description { get; private set; }

        [StringLength(1000)]
        public string? ImageUrl { get; private set; }

        public bool IsActive { get; private set; }

        #endregion

        #region İlişkiler ve Hiyerarşi

        public int? ParentCategoryId { get; private set; }

        [ForeignKey("ParentCategoryId")]
        public virtual Category? ParentCategory { get; private set; }

        public virtual ICollection<Category> SubCategories { get; private set; } = new List<Category>();

        public virtual ICollection<Product.Product> Products { get; private set; } = new List<Product.Product>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private Category() { }

        public Category(string name, string slug, int? parentCategoryId = null, string? description = null, string? imageUrl = null)
        {
            UpdateDetails(name, slug, description);
            ChangeParent(parentCategoryId);
            UpdateImageUrl(imageUrl);

            IsActive = true;
        }

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

        public void ChangeParent(int? newParentCategoryId)
        {
            if (newParentCategoryId.HasValue && newParentCategoryId.Value <= 0)
                throw new ArgumentException("Geçersiz ebeveyn kategori kimliği.", nameof(newParentCategoryId));

            ParentCategoryId = newParentCategoryId;
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
        }

        #endregion
    }
}