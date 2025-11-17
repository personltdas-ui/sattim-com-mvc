using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
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
                .AsNoTracking(); // Bu sorgu sadece okuma amaçlıdır

            // Etikete göre filtrele (Gerekliyse)
            if (!string.IsNullOrWhiteSpace(tagSlug))
            {
                query = query.Where(p => p.BlogPostTags.Any(t => t.Tag.Slug == tagSlug));
            }

            // Toplam sayıyı al
            var totalCount = await query.CountAsync();

            // Sayfalama ve gerekli verileri 'Include' etme
            var posts = await query
                .OrderByDescending(p => p.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Author) // Yazar adını almak için
                .ToListAsync(); // Resimler zaten modelde (virtual ICollection)

            return (posts, totalCount);
        }

        public async Task<BlogPost?> GetPublishedPostBySlugWithDetailsAsync(string slug)
        {
            // 'AsNoTracking()' KULLANILMADI! Servis 'ViewCount'u güncelleyecek.
            return await _dbSet
                .Where(p => p.Slug == slug && p.Status == BlogPostStatus.Published)
                .Include(p => p.Author) // SADECE Yazarı yükle
                .Include(p => p.Comments
                    .Where(c => c.IsApproved)) // SADECE onaylanmış yorumları yükle
                    .ThenInclude(c => c.User) // Yorum yapanın bilgilerini yükle
                .FirstOrDefaultAsync();
        }

        public async Task<List<BlogPost>> GetAllPostsForAdminAsync()
        {
            return await _dbSet
                .Include(p => p.Author) // Yazar adını almak için
                .OrderByDescending(p => p.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<BlogPost?> GetPostForEditAsync(int id)
        {
            // Düzenleme için varlığı 'Takip Et' (Track), AsNoTracking KULLANMA!
            return await _dbSet
                .Include(p => p.BlogPostTags) // Mevcut etiket ilişkilerini yükle
                    .ThenInclude(bt => bt.Tag) // Etiketlerin isimlerini yükle
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}