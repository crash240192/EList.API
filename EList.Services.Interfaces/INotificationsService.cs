using System.Net.WebSockets;
using EList.Common.Models;
using EList.Models.Notifications;

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


        Task<CommandResult> NotifyEventCreatedAsync(Guid eventId);
        Task<CommandResult> NotifyEventCreatedAsync(Guid eventId, List<Guid> subscribers);
        Task<CommandResult> NotifyEventUpdatedAsync(Guid eventId);
        Task<CommandResult> NotifyEventCancelledAsync(Guid eventId);

        Task<CommandResult> NotifyUsersInvitedAsync(Guid eventId, List<Guid> subscribers);

        Task<CommandResult> NotifyParticipatedAsync(Guid eventId);
        Task<CommandResult> NotifyEventLeftAsync(Guid eventId);


        Task<CommandResult> NotifySubscribedAsync(Guid subscribedToId);
        Task<CommandResult> NotifyUnsubscribedAsync(Guid unsubscribedFromId);

        Task<CommandResult> NotifyAddedToBlackListAsync(Guid eventId, List<Guid> blackList);
        Task<CommandResult> NotifyNotInWhiteListAsync(Guid eventId, List<Guid> notInWhiteList);


        Task<CommandResult> NotifyNewEventRatingAsync(Guid eventId, Guid ratingItem);
        Task<CommandResult> NotifyEventRatingChangedAsync(Guid eventId, Guid ratingItem);
        Task<CommandResult> NotifyEventRatingDeletedAsync(Guid eventId);

        //Task<CommandResult> NotifyUserByContactAsync(SystemNotificationType notificationType);
    }
}
