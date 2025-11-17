using Sattim.Web.Models.Dispute;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Dispute (İhtilaf) varlığı için jenerik repository'ye EK OLARAK
    /// 'Product' ve 'Escrow' (Alıcı/Satıcı) bilgilerini içeren
    /// özel sorgu metotları sağlar.
    /// </summary>
    public interface IDisputeRepository : IGenericRepository<Models.Dispute.Dispute>
    {
        /// <summary>
        /// Bir kullanıcının dahil olduğu (Alıcı veya Satıcı olarak)
        /// tüm ihtilafları, ilgili ürün bilgileriyle birlikte getirir.
        /// </summary>
        Task<List<Models.Dispute.Dispute>> GetDisputesForUserAsync(string userId);

        /// <summary>
        /// Tek bir ihtilafın tüm detaylarını (Ürün, Escrow, Mesajlar, Gönderenler)
        /// güvenlik kontrolü için getirir.
        /// </summary>
        Task<Models.Dispute.Dispute?> GetDisputeWithDetailsAsync(int disputeId);

        Task<List<Models.Dispute.Dispute>> GetPendingDisputesForAdminAsync();
    }
}