using Sattim.Web.ViewModels.Management;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Dashboard (Admin Paneli) için gereken tüm istatistikleri ve akışları
    /// tek bir yerden, optimize edilmiş sorgularla sağlayan özel repository.
    /// </summary>
    public interface IDashboardRepository
    {
        /// <summary>
        /// Tüm KPI, Moderasyon ve Akış verilerini tek bir sorguda (veya
        /// optimize edilmiş sıralı sorgularda) çeker.
        /// </summary>
        Task<AdminDashboardViewModel> GetDashboardDataAsync();
    }
}