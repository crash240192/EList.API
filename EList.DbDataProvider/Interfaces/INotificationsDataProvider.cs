using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface INotificationsDataProvider
    {
        Task<SystemNotificationDto?> GetNotificationByTypeAsync(SystemNotificationType type);
        Task<List<NotificationDto>> GetUnreadedUserNotificationsAsync(Guid accountId);
        Task<ListResponse<NotificationDto>> SearchUserNotificationsAsync(
            Guid accountId,
            string? type,
            bool unreadOnly,
            int pageIndex,
            int pageSize);
        Task<int> CountUserNotificationsAsync(Guid accountId, string? type, bool unreadOnly);

        Task ReadNotificationAsync(Guid notificationId);
        Task ReadAllUserNotificationsAsync(Guid accountId);

        Task<Guid> CreateNotificationAsync(NotificationDto notification);
        Task CreateNotificationsAsync(List<NotificationDto> notifications);


        Task<List<Guid>> SearchSubscribersEventCreatedAsync(Guid creatorId);
    }
}
