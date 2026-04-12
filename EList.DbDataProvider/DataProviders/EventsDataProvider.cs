using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using LinqToDB;

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
            var eventItem = await _connection.Events.FirstOrDefaultAsync(i => i.Id == id);
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
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
                .UpdateAsync();
        }

        public async Task<(int, List<EventDto>)> SearchEventsAsync(EventsSearchRequest request)
        {
            var eventParametersRequest = _connection.EventParameters.AsQueryable();
            var eventTypes = _connection.EventTypes.AsQueryable();
            var eventsRequest = _connection.Events
                .LoadWith(i => i.Organizator)
                .LoadWith(i => i.Parameters)
                .LoadWith(i => i.Participants)
                .LoadWith(i => i.Types)
                .Where(i => request.StartTime != null ? i.StartTime >= request.StartTime : true)
                .Where(i => request.EndTime != null ? i.StartTime <= request.EndTime : true).AsQueryable();

            #region parameters
            List<Guid> eventParameterIds = null;

            if (request.AllowedGender != null)
                eventParametersRequest = eventParametersRequest.Where(i => i.AllowedGender == request.AllowedGender);

            if (request.Price != null)
                eventParametersRequest = eventParametersRequest.Where(i => i.Cost <= request.Price);
            #endregion

            #region eventTypes
            if (request.Types?.Any() ?? false)
                eventTypes = eventTypes.Where(i => request.Types.Contains(i.Id));


            //if (request.Categories?.Any() ?? false)
            //{
            //    var eventTypesByCategories = _connection.EventCategories.Where(i => request.Categories.Contains(i.Id)).SelectMany(i => i.);
            //}
                

            if (request.Categories?.Any() ?? false)
                eventTypes = eventTypes.Where(i => request.Categories.Contains(i.EventCategoryId));

            var eventTypeIdsByTypes = await eventTypes.Select(i => i.Id).ToListAsync();
            #endregion

            #region main event data
            var nameSubstrings = request.Name?.Split(' ').Where(i => i.Length > 0).ToList() ?? null;

            if (nameSubstrings?.Count() > 0)
                eventsRequest = eventsRequest.Where(i => nameSubstrings.TrueForAll(name => i.Name.ToLower().Contains(name.ToLower())));

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
                    eventsRequest = eventsRequest.Where(i => i.Organizator.AccountId == request.OrganizatorId || i.Participants.Any(p => p.AccountId == request.ParticipantId));
                else
                    eventsRequest = eventsRequest.Where(i => i.Organizator.AccountId == request.OrganizatorId);
            }
            else if (request.ParticipantId != null)
            {
                eventsRequest = eventsRequest.Where(i => i.Participants.Any(p => p.AccountId == request.ParticipantId));
            }
            #endregion

            var totalCount = await eventsRequest.CountAsync();

            var resultList = await eventsRequest.Skip(request.PageSize * (request.PageIndex)).Take(request.PageSize).ToListAsync();

            return (totalCount, resultList);
        }
    }
}
