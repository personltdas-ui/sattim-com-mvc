using Sattim.Web.Models.Shipping;
using Sattim.Web.Services.Repositories;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// ShippingInfo varlığı için jenerik repository'ye EK OLARAK
    /// 'Escrow' (Alıcı/Satıcı) bilgilerini içeren
    /// özel sorgu metotları sağlar.
    /// </summary>
    public interface IShippingRepository : IGenericRepository<ShippingInfo>
    {
        /// <summary>
        /// Tek bir kargo bilgisini, ilişkili Escrow (Alıcı/Satıcı ID'leri için)
        /// ile birlikte 'takip ederek' (tracking) getirir.
        /// (Update işlemleri için kullanılır)
        /// </summary>
        Task<ShippingInfo?> GetShippingInfoForUpdateAsync(int productId);

        /// <summary>
        /// Tek bir kargo bilgisini, ilişkili Escrow (Alıcı/Satıcı ID'leri için)
        /// ile birlikte 'takip etmeden' (NoTracking) getirir.
        /// (Sadece okuma (Query) işlemleri için kullanılır)
        /// </summary>
        Task<ShippingInfo?> GetShippingInfoDetailsAsync(int productId);
    }
}