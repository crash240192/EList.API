using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

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

        public async Task<ListResponse<NotificationDto>> SearchUserNotificationsAsync(
            Guid accountId,
            int? type,
            bool unreadOnly,
            int pageIndex,
            int pageSize)
        {
            var query = ApplyUserNotificationFilters(_connection.UserNotifications.AsQueryable(), accountId, type, unreadOnly);
            query = query.OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync();
            var pageSz = Math.Max(pageSize, 1);
            var items = await query.Skip(pageIndex * pageSz).Take(pageSz).ToListAsync();
            return new ListResponse<NotificationDto>(totalCount, items);
        }

        public async Task<int> CountUserNotificationsAsync(Guid accountId, int? type, bool unreadOnly)
        {
            return await ApplyUserNotificationFilters(_connection.UserNotifications.AsQueryable(), accountId, type, unreadOnly)
                .CountAsync();
        }

        private static IQueryable<NotificationDto> ApplyUserNotificationFilters(
            IQueryable<NotificationDto> query,
            Guid accountId,
            int? type,
            bool unreadOnly)
        {
            query = query.Where(i => i.AccountId == accountId);
            if (unreadOnly)
                query = query.Where(i => i.ReadAt == null);
            if (type != null)
                query = query.Where(i => i.Type == type);
            return query;
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

        public async Task CreateNotificationsAsync(List<NotificationDto> notifications)
        {
            notifications.Where(i => i.Id == Guid.Empty)?.ToList().ForEach(i => i.Id = Guid.NewGuid());
            
            await _connection.BulkCopyAsync(new BulkCopyOptions
            {
                BulkCopyType = BulkCopyType.MultipleRows
            }, notifications);
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
