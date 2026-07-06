using EList.DbDataProvider.Extensions;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class EventsDataProvider : DataProviderBase, IEventsDataProvider
    {
        public EventsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateEventAsync(EventDto item)
        {
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task<EventDto> GetEventAsync(Guid id)
        {
            var eventItem = await _connection.Events
                .LoadWith(i => i.Types)
                .ThenLoad(i => i.Type)
                .ThenLoad(i => i.EventCategory)
                .LoadWith(i => i.Parameters)
                .FirstOrDefaultAsync(i => i.Id == id);
            return eventItem;
        }

        public async Task UpdateEventAsync(EventDto item)
        {
            var eventItem = await _connection.Events.Where(i => i.Id == item.Id)
                .Set(i => i.Address, item.Address)
                .Set(i => i.Active, item.Active)
                .Set(i => i.Description, item.Description)
                .Set(i => i.EndTime, item.EndTime)
                .Set(i => i.Latitude, item.Latitude)
                .Set(i => i.Longitude, item.Longitude)
                .Set(i => i.Name, item.Name)
                .Set(i => i.StartTime, item.StartTime)
                .Set(i => i.CoverImageId, item.CoverImageId)
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
                .UpdateAsync();
        }

        public async Task SetEventCoverImageAsync(Guid eventId, Guid? imageId)
        {
            var eventItem = await _connection.Events.Where(i => i.Id == eventId)
                .Set(i => i.CoverImageId, imageId)
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
                .UpdateAsync();
        }

        public async Task<ListResponse<EventDto>> SearchEventsAsync(EventsSearchRequest request, Guid? curAccountId = null)
        {
            var eventParametersRequest = _connection.EventParameters.AsQueryable();
            var eventTypes = _connection.EventTypes.AsQueryable();
            var eventsRequest = _connection.Events
                .LoadWith(i => i.Organizators)
                .LoadWith(i => i.Parameters)
                .LoadWith(i => i.Participants)
                .LoadWith(i => i.Types)
                .ThenLoad(i => i.Type)
                .ThenLoad(i => i.EventCategory)
                .LoadWith(i => i.Invitations)
                .Where(i => request.StartTime != null ? i.EndTime >= request.StartTime : true)
                .Where(i => request.EndTime != null ? i.EndTime <= request.EndTime : true)
                .Where(i => request.Active ? i.Active == true : true)
                .OrderBy(i => i.StartTime)
                .AsQueryable();
            //.Where(i => request.LocationRange != null ? ;

            //if (request.OnlyWithAlbums)
            //{
            //    eventsRequest = eventsRequest
            //        .LoadWith(i => i.Albums)
            //}

            #region location
            /*
            //Вариант поиска по кругу, но использует подзапрос.
            if (request.Latitude != null && request.Longitude != null && request.LocationRange != null)
            {
                var lat = request.Latitude.Value;
                var lng = request.Longitude.Value;
                var radius = request.LocationRange.Value; // в метрах

                // Получаем ID событий в радиусе через сырой SQL
                var nearbyIds = await _connection.QueryToListAsync<Guid>(@"
                    SELECT id FROM events
                    WHERE ST_DWithin(
                        location::geography,
                        ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography,
                        @radius
                    )",
                    new { lat, lng, radius });

                eventsRequest = eventsRequest.Where(e => nearbyIds.Contains(e.Id));
            }
            */

            //Вариант без подзапроса, но ищет по квадрату, а не по кругу.
            if (request.Latitude != null && request.Longitude != null && request.LocationRange != null)
            {
                var lat = request.Latitude.Value;
                var lng = request.Longitude.Value;
                var radiusKm = request.LocationRange.Value / 1000.0;

                // Грубый bbox-фильтр через обычные колонки (быстро, с индексом)
                // 1 градус широты ≈ 111 км
                var latDelta = radiusKm / 111.0;
                var lngDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

                eventsRequest = eventsRequest
                    .Where(e => e.Latitude >= lat - latDelta && e.Latitude <= lat + latDelta)
                    .Where(e => e.Longitude >= lng - lngDelta && e.Longitude <= lng + lngDelta);
            }
            #endregion

            #region parameters
            if (request.AllowedGender != null)
                eventsRequest = eventsRequest.Where(i => i.Parameters.AllowedGender == null || i.Parameters.AllowedGender == request.AllowedGender);

            //Добавить сюда проверку что пользователь без пола или запрещённого пола не может видеть мероприятие

            if (request.Price != null)
                eventsRequest = eventsRequest.Where(e => e.Parameters.Cost == null || e.Parameters.Cost <= request.Price);

            if (request.AgeLimit != null)
                eventsRequest = eventsRequest.Where(e => e.Parameters.AgeLimit == null || e.Parameters.AgeLimit <= request.AgeLimit);

            // Отображение частных мероприятий
            // для черных списков - показывать если пользователь не в черных списках или он организатор
            // для белых списков - показывать, если пользователь в белом списке, или белый список пуст, или он организатор, или он уже участник
            if (curAccountId != null)
                eventsRequest = eventsRequest
                        .LoadWith(i => i.BlackList)
                        .LoadWith(i => i.WhiteList)
                    .Where(i =>
                        (i.Parameters.Private == true &&
                                (
                                    (i.WhiteList.Any(p => p.AccountId == curAccountId) || i.WhiteList.Count() == 0) 
                                    &&
                                    (i.Invitations.Any(inv => inv.InvitedAccountId == curAccountId) || i.Participants.Any(p => p.AccountId == curAccountId))
                                )
                        )
                        || (i.Parameters.Private != true && (!i.BlackList.Any(p => p.AccountId == curAccountId)))
                        || i.Organizators.Any(o => o.AccountId == curAccountId));
            #endregion

            #region eventTypes
            if (request.Types?.Any() ?? false)
                eventTypes = eventTypes.Where(i => request.Types.Contains(i.Id));

            if (request.Categories?.Any() ?? false)
                eventTypes = eventTypes.Where(i => request.Categories.Contains(i.EventCategoryId));

            var eventTypeIdsByTypes = await eventTypes.Select(i => i.Id).ToListAsync();
            #endregion

            #region main event data
            var nameSubstrings = request.Name?.Split(' ').Where(i => i.Length > 0).ToList() ?? null;

            if (nameSubstrings?.Count() > 0)
                eventsRequest = eventsRequest.Where(i => nameSubstrings.All(name => i.Address.ToLower().Contains(name.ToLower()) || i.Name.ToLower().Contains(name.ToLower())));

            var eventIdsByEventTypes = await _connection.EventTypeRelations.Where(i => eventTypeIdsByTypes
                .Contains(i.EventTypeId))
                .Select(i => i.EventId)
                .ToListAsync();
            eventsRequest = eventsRequest.Where(i => eventIdsByEventTypes.Contains(i.Id));
            #endregion

            #region organizator and participant
            if (request.OrganizatorId != null)
            {
                if (request.ParticipantId != null)
                    eventsRequest = eventsRequest.Where(i => i.Organizators.Any(o => o.AccountId == request.OrganizatorId) || i.Participants.Any(p => p.AccountId == request.ParticipantId));
                else
                    eventsRequest = eventsRequest.Where(i => i.Organizators.Any(o => o.AccountId == request.OrganizatorId));
            }
            else if (request.ParticipantId != null)
            {
                eventsRequest = eventsRequest.Where(i => i.Participants.Any(p => p.AccountId == request.ParticipantId));
            }
            #endregion

            var totalCount = await eventsRequest.CountAsync();

            var resultList = await eventsRequest.ToPagedQuery(request.PageIndex ?? 0, request.PageSize ?? totalCount).ToListAsync();

            return new ListResponse<EventDto>(totalCount, resultList);
        }

        public async Task CancelEventAsync(Guid eventId)
        {
            await _connection.Events.Where(i => i.Id == eventId)
                .Set(i => i.Active, false)
                .UpdateAsync();
        }
    }
}
