using Sattim.Web.Models.UI; // Notification için
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Notification varlığı için jenerik repository'ye EK OLARAK
    /// toplu okuma, toplu güncelleme ve sayım işlemleri sağlar.
    /// </summary>
    public interface INotificationRepository : IGenericRepository<Models.UI.Notification>
    {
        /// <summary>
        /// Bir kullanıcının tüm bildirimlerini (veya sadece okunmamış olanları)
        /// tarihe göre sıralı getirir.
        /// </summary>
        Task<List<Models.UI.Notification>> GetNotificationsForUserAsync(string userId, bool unreadOnly);

        /// <summary>
        /// Bir kullanıcının okunmamış bildirim sayısını (hızlı sorgu) getirir.
        /// </summary>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Bir kullanıcının tüm okunmamış bildirimlerini 'Okundu' olarak
        /// toplu (batch) günceller.
        /// </summary>
        /// <returns>Etkilenen satır sayısı.</returns>
        Task<int> MarkAllAsReadAsync(string userId);
    }
}