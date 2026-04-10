using EList.Common.Models;
using EList.Models.Enums;

namespace EList.Services.Interfaces
{
    public interface INotificationsService
    {
        Task<CommandResult> NotifyUserByContactAsync(SystemNotificationType notificationType);
    }
}
