using EList.Common.Models;
using EList.Models.Enums;

namespace EList.Services.Interfaces
{
    public interface ISystemNotificationsService
    {
        Task<CommandResult<string>> NotifyUserByContactAsync(SystemNotificationType notificationType, Guid? accountId = null);
    }
}
