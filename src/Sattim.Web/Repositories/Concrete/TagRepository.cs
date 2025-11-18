using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Blog;
using Sattim.Web.Repositories.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Tag>> GetTagsWithPublishedPostCountAsync()
        {
            return await _dbSet
                .Include(t => t.BlogPostTags)
                    .ThenInclude(bt => bt.BlogPost)
                .Where(t => t.BlogPostTags.Any(bt => bt.BlogPost.Status == BlogPostStatus.Published))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Tag>> GetAllTagsWithPostCountAsync()
        {
            return await _dbSet
                .Include(t => t.BlogPostTags)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}