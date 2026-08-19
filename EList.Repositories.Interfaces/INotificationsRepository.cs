using EList.Common.Models;
using EList.DbDataProvider.Models;
using EList.Models.Enums;
using EList.Models.Notifications;

namespace EList.Repositories.Interfaces
{
    public interface INotificationsRepository
    {
        Task<SystemNotification?> GetNotificationByTypeAsync(SystemNotificationType type);
        Task<SystemNotification?> GetSystemNotificationByIdAsync(Guid id);
        Task<List<SystemNotification>> GetAllSystemNotificationsAsync();
        Task<Guid> CreateSystemNotificationAsync(SystemNotification item);
        Task UpdateSystemNotificationAsync(SystemNotification item);
        Task DeleteSystemNotificationAsync(Guid id);

        Task<List<Notification>?> GetUnreadedUserNotificationsAsync(Guid accountId);
        Task<PagedList<Notification>> SearchUserNotificationsAsync(
            Guid accountId,
            UserNotificationType? type,
            bool unreadOnly,
            int pageIndex,
            int pageSize);
        Task<int> CountUserNotificationsAsync(Guid accountId, UserNotificationType? type, bool unreadOnly);
        Task ReadNotificationAsync(Guid notificationId);
        Task ReadAllUserNotificationsAsync(Guid accountId);
        Task<Guid> CreateNotificationAsync(Notification notification);
        Task CreateNotificationsAsync(List<Notification> notifications);


        Task<List<Guid>> SearchSubscribersEventCreatedAsync(Guid creatorId);
    }
}
