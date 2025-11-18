using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Sattim.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected string GetRequiredUserId()
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Bu işlem için kullanıcı girişi gereklidir (Controller hatası).");
            }
            return userId;
        }
    }
}