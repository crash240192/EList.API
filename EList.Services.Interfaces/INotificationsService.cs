using System.Net.WebSockets;
using EList.Common.Models;
using EList.Models.ContentReports;
using EList.Models.Enums;
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
        Task<CommandResult<PagedList<Notification>>> GetMyNotificationsAsync(NotificationsSearchRequest? request = null);
        Task<CommandResult<int>> CountMyNotificationsAsync(UserNotificationType? type = null, bool unreadOnly = true);


        Task<CommandResult> NotifyEventCreatedAsync(Guid eventId, List<Guid> subscribers = null);
        Task<CommandResult> NotifyEventUpdatedAsync(Guid eventId);
        Task<CommandResult> NotifyEventCancelledAsync(Guid eventId);

        Task<CommandResult> NotifyUsersInvitedAsync(Guid eventId, List<Guid> subscribers);
        Task<CommandResult> NotifyInvitationAcceptedAsync(Guid eventId, Guid invitedAccountId, Guid? inviterAccountId);
        Task<CommandResult> NotifyInvitationDeclinedAsync(Guid eventId, Guid invitedAccountId, Guid? inviterAccountId);
        Task<CommandResult> NotifyInvitationCancelledAsync(Guid eventId, Guid invitedAccountId);

        Task<CommandResult> NotifyParticipatedAsync(Guid eventId);
        Task<CommandResult> NotifyEventLeftAsync(Guid eventId);
        Task<CommandResult> NotifyRemovedFromEventAsync(Guid eventId, List<Guid> accountIds);


        Task<CommandResult> NotifySubscribedAsync(Guid subscribedToId);
        Task<CommandResult> NotifyUnsubscribedAsync(Guid unsubscribedFromId);

        Task<CommandResult> NotifyAddedToBlackListAsync(Guid eventId, List<Guid> blackList);
        Task<CommandResult> NotifyAddedToWhiteListAsync(Guid eventId, List<Guid> whiteList);
        Task<CommandResult> NotifyRemovedFromBlackListAsync(Guid eventId, List<Guid> accountIds);
        Task<CommandResult> NotifyRemovedFromWhiteListAsync(Guid eventId, List<Guid> accountIds);
        Task<CommandResult> NotifyNotInWhiteListAsync(Guid eventId, List<Guid> notInWhiteList);


        Task<CommandResult> NotifyNewEventRatingAsync(Guid eventId, Guid ratingItem, List<Guid> organizators = null);
        Task<CommandResult> NotifyEventRatingChangedAsync(Guid eventId, Guid ratingItem, List<Guid> organizators = null);
        Task<CommandResult> NotifyEventRatingDeletedAsync(Guid eventId, List<Guid> organizators = null);


        Task<CommandResult> NotifyCommentRepliedAsync(Guid? eventId, Guid messageId, Guid replyId);
        /// <summary>Устаревшее имя; используйте <see cref="NotifyCommentRepliedAsync"/>.</summary>
        Task<CommandResult> NotifyCommentRepliedsync(Guid? eventId, Guid messageId, Guid replyId);
        Task<CommandResult> NotifyNewMessageAsync(Guid conversationId, Guid messageId, Guid? eventId = null);

        Task<CommandResult> NotifyContentReportCreatedAsync(ContentReport report);
        Task<CommandResult> NotifyContentReportResolvedAsync(
            ContentReport report,
            ReportResolutionAction action,
            string? comment);
        Task<CommandResult> NotifyContentReportEscalatedAsync(ContentReport report);
        Task<CommandResult> NotifyContentReportPenaltyIssuedAsync(ModerationPenalty penalty);
        Task<CommandResult> NotifyEventRestoredAsync(Guid eventId);

        Task<CommandResult> NotifyOrganizationMemberAddedAsync(Guid organizationId, Guid accountId);
        Task<CommandResult> NotifyOrganizationMemberRemovedAsync(Guid organizationId, Guid accountId);
        Task<CommandResult> NotifyOrganizationMemberDeactivatedAsync(Guid organizationId, Guid accountId);
        Task<CommandResult> NotifyOrganizationOwnershipTransferredAsync(Guid organizationId, Guid newOwnerAccountId, Guid previousOwnerAccountId);
        Task<CommandResult> NotifyOrganizationVerificationApprovedAsync(Guid organizationId);
        Task<CommandResult> NotifyOrganizationVerificationRejectedAsync(Guid organizationId, string? reason);

        Task<CommandResult> NotifyEventOrganizatorAssignedAsync(Guid eventId, List<Guid> accountIds);
        Task<CommandResult> NotifyEventOrganizatorRemovedAsync(Guid eventId, Guid accountId);

        Task<CommandResult> NotifyAgreementUpdateRequiredAsync(DocumentType documentType, string version, List<Guid> accountIds);
        //Task<CommandResult> NotifyUserByContactAsync(SystemNotificationType notificationType);
    }
}
