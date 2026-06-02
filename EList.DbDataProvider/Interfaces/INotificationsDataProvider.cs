using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface INotificationsDataProvider
    {
        Task<SystemNotificationDto?> GetNotificationByTypeAsync(SystemNotificationType type);
        Task<List<NotificationDto>> GetUnreadedUserNotificationsAsync(Guid accountId);

        Task ReadNotificationAsync(Guid notificationId);
        Task ReadAllUserNotificationsAsync(Guid accountId);

        Task<Guid> CreateNotificationAsync(NotificationDto notification);


        Task<List<Guid>> SearchSubscribersEventCreatedAsync(Guid creatorId);
    }
}
