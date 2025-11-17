using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Blog
{
    

    /// <summary>
    /// PostCommentAsync metodu için yeni yorum formu verisi.
    /// </summary>
    public class PostCommentViewModel
    {
        [Required]
        public int BlogPostId { get; set; }

        [Required(ErrorMessage = "Yorum alanı boş olamaz.")]
        [StringLength(2000, MinimumLength = 10)]
        [Display(Name = "Yorumunuz")]
        public string Content { get; set; }
    }

    

    /// <summary>
    /// Blog ana sayfasındaki bir yazı özetini temsil eder.
    /// (GetPublishedPostsAsync tarafından döndürülür)
    /// </summary>
    public class BlogSummaryViewModel
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Excerpt { get; set; }
        public string FeaturedImage { get; set; }
        public DateTime PublishedDate { get; set; }
        public string AuthorName { get; set; }
    }

    /// <summary>
    /// Tek bir blog yazısı detayını temsil eder.
    /// (GetPostBySlugAsync tarafından döndürülür)
    /// </summary>
    public class BlogPostDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Slug { get; set; }
        public DateTime PublishedDate { get; set; }
        public int ViewCount { get; set; }
        public string FeaturedImage { get; set; }

        public BlogAuthorViewModel Author { get; set; }
        public List<BlogCommentViewModel> Comments { get; set; } = new List<BlogCommentViewModel>();
    }

    /// <summary>
    /// Blog yazarı detaylarını temsil eder (BlogPostDetailViewModel içinde kullanılır).
    /// </summary>
    public class BlogAuthorViewModel
    {
        public string FullName { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Bio { get; set; } // (UserProfile'dan gelir)
    }

    /// <summary>
    /// Onaylanmış bir yorumu temsil eder (BlogPostDetailViewModel içinde kullanılır).
    /// </summary>
    public class BlogCommentViewModel
    {
        public string AuthorFullName { get; set; }
        public string AuthorProfileImageUrl { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Etiket bulutundaki tek bir etiketi temsil eder.
    /// (GetTagCloudAsync tarafından döndürülür)
    /// </summary>
    public class BlogTagCloudViewModel
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public int PostCount { get; set; } 
    }
}