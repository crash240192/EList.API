using System.Net.WebSockets;
using EList.Common.Models;
using EList.Models.Enums;
using EList.Models.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace EList.Services.Interfaces
{
    public interface INotificationsService
    {
        Task<CommandResult> AddConnectionAsync(Guid accountId, WebSocket socket);
        Task<CommandResult> AddConnectionAsync(WebSocket socket);

        CommandResult<ConnectionStats> GetConnectionStats();

        Task<CommandResult> HandleNewNotificationAsync(Notification notification);
        Task<CommandResult> SendToUserAsync(Guid accountId, Notification notification);
        Task<CommandResult> BroadcastAsync(Notification request);


        Task<CommandResult> ReadNotificationAsync(Guid notificationId);
        Task<CommandResult> ReadAllUserNotificationsAsync();


        Task<CommandResult> NotifyEventCreatedAsync(Guid creatorId, Guid eventId);
        Task<CommandResult> NotifyEventCreatedAsync(Guid creatorId, Guid eventId, List<Guid> subscribers);
        Task<CommandResult> NotifyUsersInvitedAsync(Guid creatorId, Guid eventId, List<Guid> subscribers);
        //Task<CommandResult> NotifyUserByContactAsync(SystemNotificationType notificationType);
    }
}
