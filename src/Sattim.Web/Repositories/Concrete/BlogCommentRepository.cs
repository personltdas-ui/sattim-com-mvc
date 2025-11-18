using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Blog;
using Sattim.Web.Repositories.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class BlogCommentRepository : GenericRepository<BlogComment>, IBlogCommentRepository
    {
        public BlogCommentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<BlogComment>> GetPendingCommentsWithDetailsAsync()
        {
            return await _dbSet
                .Where(c => !c.IsApproved)
                .Include(c => c.User)
                .Include(c => c.BlogPost)
                .OrderByDescending(c => c.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}