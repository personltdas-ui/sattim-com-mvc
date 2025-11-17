using Microsoft.Extensions.Logging;
using Sattim.Web.Services.Repositories; // Özel Repository için
using Sattim.Web.ViewModels.Management; // ViewModel'lar için
using System;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Management
{
    public class DashboardService : IDashboardService
    {
        // DÜZELTME: DbContext yerine IDashboardRepository enjekte edildi.
        private readonly IDashboardRepository _dashboardRepo;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IDashboardRepository dashboardRepo,
            ILogger<DashboardService> logger)
        {
            _dashboardRepo = dashboardRepo;
            _logger = logger;
        }

        public async Task<AdminDashboardViewModel> GetDashboardStatisticsAsync()
        {
            _logger.LogInformation("Admin Dashboard istatistikleri alınıyor...");

            try
            {
                // DÜZELTME: Tüm sorgu mantığı Repository'ye devredildi.
                // Servis'in tek görevi, repoyu çağırmak ve (gerekirse)
                // hata durumunu yönetmektir.
                var model = await _dashboardRepo.GetDashboardDataAsync();

                _logger.LogInformation("Admin Dashboard istatistikleri başarıyla alındı.");
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Dashboard istatistikleri alınırken kritik bir hata oluştu.");

                // Hata durumunda, sayfanın çökmesini engellemek için
                // boş (ama geçerli) bir model döndür.
                return new AdminDashboardViewModel
                {
                    TotalUsers = -1, 
                    TotalSalesVolume = 0,
                    
                };
            }
        }
    }
}