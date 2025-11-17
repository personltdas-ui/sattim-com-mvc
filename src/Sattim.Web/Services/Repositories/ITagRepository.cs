using Sattim.Web.Models.Blog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Tag (Etiket) varlığı için jenerik repository'ye EK OLARAK
    /// 'Etiket Bulutu' (Tag Cloud) gibi özel sorgu metotları sağlar.
    /// </summary>
    public interface ITagRepository : IGenericRepository<Tag>
    {
        /// <summary>
        /// Tüm etiketleri, ilişkili 'Yayınlanmış' yazı sayılarıyla birlikte getirir.
        /// </summary>
        Task<List<Tag>> GetTagsWithPublishedPostCountAsync();

        /// <summary>
        /// Admin paneli için TÜM etiketleri ve yazı sayılarını (Taslaklar dahil) getirir.
        /// </summary>
        Task<List<Tag>> GetAllTagsWithPostCountAsync();
    }
}