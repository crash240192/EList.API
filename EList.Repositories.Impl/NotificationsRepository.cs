using AutoMapper;
using EList.DbDataProvider.Interfaces;
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

        public async Task<SystemNotification?> GetNotificationByTypeAsync(SystemNotificationType type)
        {
            var notificationTypeMapped = _mapper.Map<DbDataProvider.Models.Enums.SystemNotificationType>(type);
            var notification = await _notificationsDataProvider.GetNotificationByTypeAsync(notificationTypeMapped);
            var result = _mapper.Map<SystemNotification>(notification);
            return result;
        }
    }
}
