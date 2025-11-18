using Sattim.Web.Models.Blog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IBlogCommentRepository : IGenericRepository<BlogComment>
    {
        Task<List<BlogComment>> GetPendingCommentsWithDetailsAsync();
    }
}