using EList.Models.Notifications;

namespace EList.Models.Notifications
{
    public class NotificationsSearchRequest
    {
        public UserNotificationType? Type { get; set; }
        public bool? UnreadOnly { get; set; }
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
    }
}
