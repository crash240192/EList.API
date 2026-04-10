using EList.Models.Enums;
using EList.Models.Notifications;

namespace EList.Repositories.Interfaces
{
    public interface INotificationsRepository
    {
        Task<SystemNotification?> GetNotificationByTypeAsync(SystemNotificationType type);
    }
}
