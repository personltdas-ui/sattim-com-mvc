using Sattim.Web.Models.User;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// ApplicationUser varlığı için jenerik repository'ye EK OLARAK
    /// kullanıcı adı, e-posta veya ad/soyad ile arama gibi
    /// özel sorgu metotları sağlar.
    /// </summary>
    public interface IUserRepository : IGenericRepository<ApplicationUser>
    {
        /// <summary>
        /// Bir kullanıcıyı normalize edilmiş kullanıcı adına (NormalizedUserName)
        /// göre bulur.
        /// </summary>
        Task<ApplicationUser?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// Bir kullanıcıyı normalize edilmiş e-posta adresine
        /// (NormalizedEmail) göre bulur.
        /// </summary>
        Task<ApplicationUser?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Kullanıcıları 'FullName' alanına göre arar (örn: admin paneli için).
        /// </summary>
        Task<IEnumerable<ApplicationUser>> SearchUsersByNameAsync(string nameQuery);
    }
}
