using Sattim.Web.Models.User;

namespace Sattim.Web.Services.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser>, IUserRepository
    {
        // 'GenericRepository' zaten _context'i 'protected' olarak
        // tanımladığı için ona erişebiliriz.
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ApplicationUser?> GetUserByUsernameAsync(string username)
        {
            var normalizedUsername = username.ToUpperInvariant();

            // _dbSet (context.Users) üzerinden sorgulama
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
