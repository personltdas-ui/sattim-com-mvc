using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Blog;
using Sattim.Web.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class BlogPostRepository : GenericRepository<BlogPost>, IBlogPostRepository
    {
        public BlogPostRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<(List<BlogPost> Posts, int TotalCount)> GetPublishedPostsPaginatedAsync(
            int page, int pageSize, string? tagSlug = null)
        {
            var query = _dbSet
                .Where(p => p.Status == BlogPostStatus.Published)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(tagSlug))
            {
                query = query.Where(p => p.BlogPostTags.Any(t => t.Tag.Slug == tagSlug));
            }

            var totalCount = await query.CountAsync();

            var posts = await query
                .OrderByDescending(p => p.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Author)
                .ToListAsync();

            return (posts, totalCount);
        }

        public async Task<BlogPost?> GetPublishedPostBySlugWithDetailsAsync(string slug)
        {
            return await _dbSet
                .Where(p => p.Slug == slug && p.Status == BlogPostStatus.Published)
                .Include(p => p.Author)
                .Include(p => p.Comments
                    .Where(c => c.IsApproved))
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync();
        }

        public async Task<List<BlogPost>> GetAllPostsForAdminAsync()
        {
            return await _dbSet
                .Include(p => p.Author)
                .OrderByDescending(p => p.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<BlogPost?> GetPostForEditAsync(int id)
        {
            return await _dbSet
                .Include(p => p.BlogPostTags)
                    .ThenInclude(bt => bt.Tag)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}