using Sattim.Web.ViewModels.Management;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Management
{
    public interface IDashboardService
    {
        /// <summary>
        /// Admin paneli ana sayfası için tüm özet istatistikleri
        /// ve moderasyon kuyruklarını veritabanından toplar.
        /// </summary>
        Task<AdminDashboardViewModel> GetDashboardStatisticsAsync();
    }
}