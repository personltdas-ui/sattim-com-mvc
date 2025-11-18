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

        [Route("/Admin")]
        [Route("/Admin/Dashboard")]
        [Route("/Admin/Dashboard/Index")]
        public async Task<IActionResult> Index()
        {
            AdminDashboardViewModel viewModel = await _dashboardService.GetDashboardStatisticsAsync();
            return View(viewModel);
        }
    }
}