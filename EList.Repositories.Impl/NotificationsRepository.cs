using AutoMapper;
using EList.Common.Models;
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

        public async Task<SystemNotification?> GetSystemNotificationByIdAsync(Guid id)
        {
            var item = await _notificationsDataProvider.GetSystemNotificationByIdAsync(id);
            return _mapper.Map<SystemNotification>(item);
        }

        public async Task<List<SystemNotification>> GetAllSystemNotificationsAsync()
        {
            var items = await _notificationsDataProvider.GetAllSystemNotificationsAsync();
            return _mapper.Map<List<SystemNotification>>(items);
        }

        public async Task<Guid> CreateSystemNotificationAsync(SystemNotification item)
        {
            var mapped = _mapper.Map<DbDataProvider.Models.SystemNotificationDto>(item);
            return await _notificationsDataProvider.CreateSystemNotificationAsync(mapped);
        }

        public async Task UpdateSystemNotificationAsync(SystemNotification item)
        {
            var mapped = _mapper.Map<DbDataProvider.Models.SystemNotificationDto>(item);
            await _notificationsDataProvider.UpdateSystemNotificationAsync(mapped);
        }

        public async Task DeleteSystemNotificationAsync(Guid id)
        {
            await _notificationsDataProvider.DeleteSystemNotificationAsync(id);
        }
        #endregion


        #region userNotifications
        public async Task<Guid> CreateNotificationAsync(Notification notification)
        {
            var mappedRequest = _mapper.Map<NotificationDto>(notification);
            var result = await _notificationsDataProvider.CreateNotificationAsync(mappedRequest);
            return result;
        }

        public async Task CreateNotificationsAsync(List<Notification> notifications)
        {
            var mapped = _mapper.Map<List<NotificationDto>>(notifications);
            await _notificationsDataProvider.CreateNotificationsAsync(mapped);
        }

        public async Task<List<Notification>?> GetUnreadedUserNotificationsAsync(Guid accountId)
        {
            var notifications = await _notificationsDataProvider.GetUnreadedUserNotificationsAsync(accountId);
            var result = _mapper.Map<List<Notification>>(notifications);
            return result;
        }

        public async Task<PagedList<Notification>> SearchUserNotificationsAsync(
            Guid accountId,
            UserNotificationType? type,
            bool unreadOnly,
            int pageIndex,
            int pageSize)
        {
            var result = await _notificationsDataProvider.SearchUserNotificationsAsync(
                accountId,
                type == null ? null : ((int)type.Value).ToString(),
                unreadOnly,
                pageIndex,
                pageSize);

            var items = _mapper.Map<List<Notification>>(result.Items ?? new List<NotificationDto>());
            return new PagedList<Notification>(
                result.TotalCount,
                items,
                pageIndex,
                pageSize);
        }

        public async Task<int> CountUserNotificationsAsync(Guid accountId, UserNotificationType? type, bool unreadOnly)
        {
            return await _notificationsDataProvider.CountUserNotificationsAsync(
                accountId,
                type == null ? null : ((int)type.Value).ToString(),
                unreadOnly);
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


        #region searchAcceptors

        public async Task<List<Guid>> SearchSubscribersEventCreatedAsync(Guid creatorId)
        {
            var result = await _notificationsDataProvider.SearchSubscribersEventCreatedAsync(creatorId);
            return result;
        }
        #endregion
    }
}
