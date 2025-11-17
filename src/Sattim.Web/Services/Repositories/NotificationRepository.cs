using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    public class NotificationRepository : GenericRepository<Models.UI.Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Models.UI.Notification>> GetNotificationsForUserAsync(string userId, bool unreadOnly)
        {
            var query = _dbSet
                .Where(n => n.UserId == userId)
                .AsNoTracking();

            if (unreadOnly)
            {
                query = query.Where(n => n.IsRead == false);
            }

            return await query
                .OrderByDescending(n => n.CreatedDate)
                .Take(50) // (Aşırı yüklenmeyi önlemek için her zaman limit koyun)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _dbSet
                .CountAsync(n => n.UserId == userId && n.IsRead == false);
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            // EF Core 7+ 'ExecuteUpdateAsync'
            // Bu, binlerce bildirimi tek bir SQL komutuyla günceller.
            // (Performans için en iyi yöntemdir)
            return await _dbSet
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadDate, DateTime.UtcNow)
                );
        }
    }
}