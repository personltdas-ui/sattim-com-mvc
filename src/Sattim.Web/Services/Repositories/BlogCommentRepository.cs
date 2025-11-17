using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Blog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    public class BlogCommentRepository : GenericRepository<BlogComment>, IBlogCommentRepository
    {
        public BlogCommentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<BlogComment>> GetPendingCommentsWithDetailsAsync()
        {
            return await _dbSet
                .Where(c => !c.IsApproved) // Onaylanmamış
                .Include(c => c.User) // Yorumu yapan
                .Include(c => c.BlogPost) // Yorum yapılan yazı
                .OrderByDescending(c => c.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}