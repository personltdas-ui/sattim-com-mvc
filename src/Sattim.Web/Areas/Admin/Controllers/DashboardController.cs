using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Services.Management;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Sattim.Web.ViewModels.Management;

namespace Sattim.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Moderator")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Admin panelinin ana sayfası.
        /// Rota: /Admin/ veya /Admin/Dashboard/Index
        /// </summary>
        [Route("/Admin")]
        [Route("/Admin/Dashboard")]
        [Route("/Admin/Dashboard/Index")]
        public async Task<IActionResult> Index()
        {
            // Servis, view için gereken tüm veriyi toplayan hazır view modeli döner.
            AdminDashboardViewModel viewModel = await _dashboardService.GetDashboardStatisticsAsync();
            return View(viewModel);
        }
    }
}