using Sattim.Web.Models.Blog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// BlogPost varlığı için jenerik repository'ye EK OLARAK
    /// sayfalanmış, filtrelenmiş ve 'Include' edilmiş
    /// özel sorgu metotları sağlar.
    /// </summary>
    public interface IBlogPostRepository : IGenericRepository<BlogPost>
    {
        /// <summary>
        /// Yayınlanmış blog yazılarını (ve ilişkili varlıklarını)
        /// sayfalanmış ve (isteğe bağlı) etikete göre filtrelenmiş olarak getirir.
        /// </summary>
        Task<(List<BlogPost> Posts, int TotalCount)> GetPublishedPostsPaginatedAsync(
            int page, int pageSize, string? tagSlug = null);

        /// <summary>
        /// Tek bir yayınlanmış yazıyı, 'Slug' (URL) kullanarak,
        /// Yazar, Profil ve ONAYLANMIŞ Yorumlar ile birlikte getirir.
        /// </summary>
        /// <remarks>
        /// ÖNEMLİ: Bu metot, 'AsNoTracking()' KULLANMAZ, çünkü servis
        /// bu varlığın 'ViewCount' özelliğini güncellemek (Update) isteyecektir.
        /// </remarks>
        Task<BlogPost?> GetPublishedPostBySlugWithDetailsAsync(string slug);

        /// <summary>
        /// Admin paneli için TÜM blog yazılarını (Taslaklar dahil) getirir.
        /// </summary>
        Task<List<BlogPost>> GetAllPostsForAdminAsync();

        /// <summary>
        /// Admin panelinde düzenleme için bir yazıyı (Taslak olabilir)
        /// Etiketleriyle (Tags) birlikte getirir.
        /// </summary>
        Task<BlogPost?> GetPostForEditAsync(int id);
    }
}