using Sattim.Web.Models.Blog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<List<Tag>> GetTagsWithPublishedPostCountAsync();

        Task<List<Tag>> GetAllTagsWithPostCountAsync();
    }
}