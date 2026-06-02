using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class NotificationsDataProvider : DataProviderBase, INotificationsDataProvider
    {
        public NotificationsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<SystemNotificationDto?> GetNotificationByTypeAsync(SystemNotificationType type)
        {
            var result = await _connection.SystemNotifications.FirstOrDefaultAsync(i => i.Type == type);
            return result;
        }

        public async Task<List<NotificationDto>> GetUnreadedUserNotificationsAsync(Guid accountId)
        {
            var result = await _connection.UserNotifications.Where(i =>  i.AccountId == accountId && i.ReadAt == null).ToListAsync();
            return result;
        }

        public async Task ReadNotificationAsync(Guid notificationId)
        {
            await _connection.UserNotifications.Where(i => i.Id == notificationId)
                .Set(i => i.ReadAt, DateTimeOffset.Now)
                .UpdateAsync();
        }

        public async Task ReadAllUserNotificationsAsync(Guid accountId)
        {
            await _connection.UserNotifications.Where(i => i.AccountId == accountId)
                .Set(i => i.ReadAt, DateTimeOffset.Now)
                .UpdateAsync();
        }

        public async Task<Guid> CreateNotificationAsync(NotificationDto notification)
        {
            var result = (Guid) await _connection.InsertWithIdentityAsync(notification);
            return result;
        }




        public async Task<List<Guid>> SearchSubscribersEventCreatedAsync(Guid creatorId)
        {
            var result = await _connection.Subscriptions
                .Where(i => i.SubscribedToId == creatorId)
                .Select(i => i.SubscriberId)
                .ToListAsync();

            return result;
        }
    }
}
