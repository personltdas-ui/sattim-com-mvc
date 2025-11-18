using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class UserRepository : GenericRepository<ApplicationUser>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ApplicationUser?> GetUserByUsernameAsync(string username)
        {
            var normalizedUsername = username.ToUpperInvariant();

            return await _dbSet
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername);
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            var normalizedEmail = email.ToUpperInvariant();

            return await _dbSet
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        }

        public async Task<IEnumerable<ApplicationUser>> SearchUsersByNameAsync(string nameQuery)
        {
            var query = nameQuery.ToLowerInvariant();

            return await _dbSet
                .Where(u => u.FullName.ToLower().Contains(query))
                .OrderBy(u => u.FullName)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}