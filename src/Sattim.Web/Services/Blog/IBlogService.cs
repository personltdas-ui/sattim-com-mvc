using Sattim.Web.Models.Blog;
using Sattim.Web.ViewModels.Blog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Blog
{
    public interface IBlogService
    {
        Task<(List<BlogSummaryViewModel> Posts, int TotalPages)> GetPublishedPostsAsync(int page = 1, int pageSize = 10, string? tagSlug = null);

        Task<BlogPostDetailViewModel> GetPostBySlugAsync(string slug);

        Task<List<BlogTagCloudViewModel>> GetTagCloudAsync();

        Task<(bool Success, string Slug, string ErrorMessage)> PostCommentAsync(PostCommentViewModel model, string userId);

    }
}