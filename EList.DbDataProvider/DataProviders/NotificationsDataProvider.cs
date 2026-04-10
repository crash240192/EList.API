using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;

namespace EList.DbDataProvider.DataProviders
{
    public class NotificationsDataProvider : DataProviderBase, INotificationsDataProvider
    {
        public NotificationsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<SystemNotificationDto?> GetNotificationByTypeAsync(SystemNotificationType type)
        {
            var result = await _connection.Notifications.FirstOrDefaultAsync(i => i.Type == type);
            return result;
        }
    }
}
