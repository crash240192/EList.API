using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Enums;
using EList.Models.Notifications;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class NotificationsRepository : INotificationsRepository 
    {
        private readonly INotificationsDataProvider _notificationsDataProvider;
        private readonly IMapper _mapper;

        public NotificationsRepository(INotificationsDataProvider notificationsDataProvider,
            IMapper mapper)
        {
            _notificationsDataProvider = notificationsDataProvider;
            _mapper = mapper;
        }

        #region systemNotifications
        public async Task<SystemNotification?> GetNotificationByTypeAsync(SystemNotificationType type)
        {
            var notificationTypeMapped = _mapper.Map<DbDataProvider.Models.Enums.SystemNotificationType>(type);
            var notification = await _notificationsDataProvider.GetNotificationByTypeAsync(notificationTypeMapped);
            var result = _mapper.Map<SystemNotification>(notification);
            return result;
        }
        #endregion


        #region userNotifications
        public async Task<Guid> CreateNotificationAsync(Notification notification)
        {
            var mappedRequest = _mapper.Map<NotificationDto>(notification);
            var result = await _notificationsDataProvider.CreateNotificationAsync(mappedRequest);
            return result;
        }

        public async Task<List<Notification>?> GetUnreadedUserNotificationsAsync(Guid accountId)
        {
            var notifications = await _notificationsDataProvider.GetUnreadedUserNotificationsAsync(accountId);
            var result = _mapper.Map<List<Notification>>(notifications);
            return result;
        }

        public async Task ReadAllUserNotificationsAsync(Guid accountId)
        {
            await _notificationsDataProvider.ReadAllUserNotificationsAsync(accountId);
        }

        public async Task ReadNotificationAsync(Guid notificationId)
        {
            await _notificationsDataProvider.ReadNotificationAsync(notificationId);
        }
        #endregion
    }
}
