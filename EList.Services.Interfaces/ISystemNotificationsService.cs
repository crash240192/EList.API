using EList.Common.Models;
using EList.Models.Enums;
using EList.Models.Notifications;

namespace EList.Services.Interfaces
{
    public interface ISystemNotificationsService
    {
        Task<CommandResult<string>> NotifyUserByContactAsync(SystemNotificationType notificationType, Guid? accountId = null);

        Task<CommandResult<List<SystemNotification>>> GetAllAsync();
        Task<CommandResult<SystemNotification?>> GetByIdAsync(Guid id);
        Task<CommandResult<Guid>> CreateAsync(SystemNotification item);
        Task<CommandResult> UpdateAsync(Guid id, SystemNotification item);
        Task<CommandResult> DeleteAsync(Guid id);
    }
}
