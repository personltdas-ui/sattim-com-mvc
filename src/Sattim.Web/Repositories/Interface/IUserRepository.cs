using Sattim.Web.Models.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IUserRepository : IGenericRepository<ApplicationUser>
    {
        Task<ApplicationUser?> GetUserByUsernameAsync(string username);

        Task<ApplicationUser?> GetUserByEmailAsync(string email);

        Task<IEnumerable<ApplicationUser>> SearchUsersByNameAsync(string nameQuery);
    }
}