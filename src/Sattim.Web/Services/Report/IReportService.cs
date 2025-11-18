using Sattim.Web.ViewModels.Report;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Report
{
    public interface IReportService
    {
        Task<(bool Success, string ErrorMessage)> CreateReportAsync(ReportFormViewModel model, string reporterId);
    }
}