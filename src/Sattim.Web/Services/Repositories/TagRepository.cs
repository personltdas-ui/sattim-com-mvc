using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Blog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Tag>> GetTagsWithPublishedPostCountAsync()
        {
            // Etiket Bulutu: Sadece yayınlanmış (Published) yazıları say
            return await _dbSet
                .Include(t => t.BlogPostTags)
                    .ThenInclude(bt => bt.BlogPost) // İlişkili BlogPost'u yükle
                .Where(t => t.BlogPostTags.Any(bt => bt.BlogPost.Status == BlogPostStatus.Published))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Tag>> GetAllTagsWithPostCountAsync()
        {
            // Admin paneli hem yayınlanmış hem taslakları sayar
            return await _dbSet
                .Include(t => t.BlogPostTags) // Tüm ilişkili yazıları say
                .AsNoTracking()
                .ToListAsync();
        }
    }
}