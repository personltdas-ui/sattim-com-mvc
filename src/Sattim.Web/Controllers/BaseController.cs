using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Sattim.Web.Controllers
{
    /// <summary>
    /// Tüm Controller'larımızın miras alacağı temel sınıf.
    /// Sık kullanılan metotları (örn: anlık kullanıcı ID'sini alma)
    /// merkezi hale getirir.
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// O an giriş yapmış olan kullanıcının ID'sini (string) alır.
        /// Eğer kullanıcı giriş yapmamışsa 'null' döndürür.
        /// </summary>
        protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// O an giriş yapmış olan kullanıcının ID'sini alır.
        /// Eğer giriş yapılmamışsa (ID 'null' ise) bir istisna (exception) fırlatır.
        /// [Authorize] attribute'u ile korunan metotlarda güvenle kullanılır.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Kullanıcı girişi bulunamadığında fırlatılır.</exception>
        protected string GetRequiredUserId()
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                // Bu durumun [Authorize] attribute'u olan bir yerde
                // normalde gerçekleşmemesi gerekir.
                throw new UnauthorizedAccessException("Bu işlem için kullanıcı girişi gereklidir (Controller hatası).");
            }
            return userId;
        }
    }
}