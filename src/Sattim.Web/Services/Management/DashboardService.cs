using Microsoft.Extensions.Logging;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.ViewModels.Management;
using System;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Management
{
    public class DashboardService : IDashboardService
    {
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
                var model = await _dashboardRepo.GetDashboardDataAsync();

                _logger.LogInformation("Admin Dashboard istatistikleri başarıyla alındı.");
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Dashboard istatistikleri alınırken kritik bir hata oluştu.");

                return new AdminDashboardViewModel
                {
                    TotalUsers = -1,
                    TotalSalesVolume = 0,
                };
            }
        }
    }
}