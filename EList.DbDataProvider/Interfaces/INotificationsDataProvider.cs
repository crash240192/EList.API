using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface INotificationsDataProvider
    {
        Task<SystemNotificationDto?> GetNotificationByTypeAsync(SystemNotificationType type);
    }
}
