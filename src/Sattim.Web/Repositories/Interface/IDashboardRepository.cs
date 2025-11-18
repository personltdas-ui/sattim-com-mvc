using Sattim.Web.ViewModels.Management;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IDashboardRepository
    {
        Task<AdminDashboardViewModel> GetDashboardDataAsync();
    }
}