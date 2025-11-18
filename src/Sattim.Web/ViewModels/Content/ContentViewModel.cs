using Sattim.Web.Models.Blog;
using Sattim.Web.Models.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Content
{
    public class SiteSettingGroupViewModel
    {
        public SettingCategory Category { get; set; }
        public List<SiteSettingUpdateViewModel> Settings { get; set; } = new List<SiteSettingUpdateViewModel>();
    }

    public class SiteSettingUpdateViewModel
    {
        [Required]
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }

    public class CategoryFormViewModel
    {
        [Required] public string Name { get; set; }
        [Required] public string Slug { get; set; }
        public int? ParentCategoryId { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class BlogPostSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string AuthorName { get; set; }
        public BlogPostStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? PublishedDate { get; set; }
    }

    public class BlogPostFormViewModel
    {
        public int Id { get; set; }
        [Required] public string Title { get; set; }
        [Required] public string Slug { get; set; }
        [Required] public string Content { get; set; }
        public string? Excerpt { get; set; }
        public string? FeaturedImage { get; set; }
        public BlogPostStatus Status { get; set; }

        public string CommaSeparatedTags { get; set; }
    }

    public class TagViewModel
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Slug { get; set; }
        public int PostCount { get; set; }
    }

    public class FaqFormViewModel
    {
        [Required] public string Question { get; set; }
        [Required] public string Answer { get; set; }
        [Required] public FAQCategory Category { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class BannerFormViewModel
    {
        [Required] public string Title { get; set; }
        [Required] public string ImageUrl { get; set; }
        public string? LinkUrl { get; set; }
        [Required] public BannerPosition Position { get; set; }
        public int DisplayOrder { get; set; }
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmailTemplateFormViewModel
    {
        [Required] public string Name { get; set; }
        [Required] public string Subject { get; set; }
        [Required] public string Body { get; set; }
        [Required] public EmailTemplateType Type { get; set; }
        public bool IsActive { get; set; }
    }
}