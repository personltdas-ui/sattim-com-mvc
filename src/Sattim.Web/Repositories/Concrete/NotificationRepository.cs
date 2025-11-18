using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.UI;
using Sattim.Web.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Notification>> GetNotificationsForUserAsync(string userId, bool unreadOnly)
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
                .Take(50)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _dbSet
                .CountAsync(n => n.UserId == userId && n.IsRead == false);
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            return await _dbSet
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadDate, DateTime.UtcNow)
                );
        }
    }
}