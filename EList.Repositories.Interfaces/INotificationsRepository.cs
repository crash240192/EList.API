using EList.DbDataProvider.Models;
using EList.Models.Enums;
using EList.Models.Notifications;

namespace EList.Repositories.Interfaces
{
    public interface INotificationsRepository
    {
        Task<SystemNotification?> GetNotificationByTypeAsync(SystemNotificationType type);
        Task<List<Notification>?> GetUnreadedUserNotificationsAsync(Guid accountId);
        Task ReadNotificationAsync(Guid notificationId);
        Task ReadAllUserNotificationsAsync(Guid accountId);
        Task<Guid> CreateNotificationAsync(Notification notification);


        Task<List<Guid>> SearchSubscribersEventCreatedAsync(Guid creatorId);
    }
}
