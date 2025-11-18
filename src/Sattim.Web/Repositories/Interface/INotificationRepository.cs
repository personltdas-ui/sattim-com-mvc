using Sattim.Web.Models.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<List<Notification>> GetNotificationsForUserAsync(string userId, bool unreadOnly);

        Task<int> GetUnreadCountAsync(string userId);

        Task<int> MarkAllAsReadAsync(string userId);
    }
}