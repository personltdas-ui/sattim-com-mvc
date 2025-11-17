using Sattim.Web.Models.Escrow;
using Sattim.Web.Services.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Escrow (Sipariş) varlığı için jenerik repository'ye EK OLARAK
    /// Alıcı/Satıcı ve Ürün/Resim bilgilerini içeren
    /// özel sorgu metotları sağlar.
    /// </summary>
    public interface IEscrowRepository : IGenericRepository<Escrow>
    {
        /// <summary>
        /// Bir alıcının tüm siparişlerini (Escrow) getirir.
        /// </summary>
        Task<List<Escrow>> GetOrdersForBuyerAsync(string buyerId);

        /// <summary>
        /// Bir satıcının tüm satışlarını (Escrow) getirir.
        /// </summary>
        Task<List<Escrow>> GetSalesForSellerAsync(string sellerId);

        /// <summary>
        /// Tek bir siparişin tüm detaylarını (Ürün, Resim, Kargo, Alıcı, Satıcı)
        /// güvenlik kontrolü için getirir.
        /// </summary>
        Task<Escrow?> GetOrderDetailAsync(int escrowProductId);
    }
}