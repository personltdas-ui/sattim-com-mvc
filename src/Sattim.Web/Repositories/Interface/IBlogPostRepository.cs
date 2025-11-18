using Sattim.Web.Models.Blog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IBlogPostRepository : IGenericRepository<BlogPost>
    {
        Task<(List<BlogPost> Posts, int TotalCount)> GetPublishedPostsPaginatedAsync(
            int page, int pageSize, string? tagSlug = null);

        Task<BlogPost?> GetPublishedPostBySlugWithDetailsAsync(string slug);

        Task<List<BlogPost>> GetAllPostsForAdminAsync();

        Task<BlogPost?> GetPostForEditAsync(int id);
    }
}