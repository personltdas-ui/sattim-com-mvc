using Sattim.Web.Models.Blog;
using Sattim.Web.Services.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// BlogComment varlığı için 'User' ve 'BlogPost'
    /// bilgilerini içeren özel sorgular sağlar.
    /// </summary>
    public interface IBlogCommentRepository : IGenericRepository<BlogComment>
    {
        Task<List<BlogComment>> GetPendingCommentsWithDetailsAsync();
    }
}