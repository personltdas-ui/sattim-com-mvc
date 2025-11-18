using Sattim.Web.ViewModels.Management;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Management
{
    public interface IDashboardService
    {
        Task<AdminDashboardViewModel> GetDashboardStatisticsAsync();
    }
}