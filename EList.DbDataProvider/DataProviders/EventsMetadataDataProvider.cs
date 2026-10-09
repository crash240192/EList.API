using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class EventsMetadataDataProvider : DataProviderBase, IEventsMetadataDataProvider
    {
        public EventsMetadataDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        #region eventType
        public async Task<Guid> CreateEventTypeAsync(EventTypeDto item)
        {
            item.Active = true;
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task DeleteEventTypeAsync(Guid id)
        {
            await _connection.EventTypes.Where(i => i.Id == id)
                .Set(i => i.Active, false)
                .UpdateAsync();
        }

        public async Task<List<EventTypeDto>?> GetAllEventTypesAsync()
        {
            var result = await _connection.EventTypes
                .Where(i => i.Active)
                .ToListAsync();

            return result;
        }

        public async Task<EventTypeDto?> GetEventTypeAsync(Guid id)
        {
            var result = await _connection.EventTypes
                .LoadWith(i => i.EventCategory)
                .Where(i => i.Id == id)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<List<EventTypeDto>> GetEventTypesByEventIdAsync(Guid eventId)
        {
            var result = await _connection.EventTypes
                .LoadWith(i => i.EventCategory)
                .LoadWith(i => i.Relations)
                .Where(i => i.Relations.Any(r => r.EventId == eventId))
                .ToListAsync();

            return result;
        }

        public async Task<List<EventTypeDto>?> GetEventTypesByCategoryIdAsync(Guid categoryId)
        {
            var result = await _connection.EventTypes
                .LoadWith(i => i.EventCategory)
                .Where(i => i.EventCategoryId == categoryId && i.Active)
                .ToListAsync();

            return result;
        }

        public async Task UpdateEventTypeAsync(EventTypeDto item)
        {
            await _connection.EventTypes.Where(i => i.Id == item.Id)
                .Set(i => i.Ico, item.Ico)
                .Set(i => i.Description, item.Description)
                .Set(i => i.Name, item.Name)
                .Set(i => i.LocalizationPath, item.LocalizationPath)
                .Set(i => i.EventCategoryId, item.EventCategoryId)
                .UpdateAsync();
        }

        public async Task BindEventTypesAsync(Guid eventId, List<Guid> newEventTypeIds)
        {
            var existingRelations = await _connection.EventTypeRelations.Where(i => i.EventId == eventId).ToListAsync();

            var newRelations = newEventTypeIds.Select(i => new EventTypeRelationDto
            {
                EventId = eventId,
                EventTypeId = i
            }).ToList();

            var relationsToRemove = existingRelations.Where(i => !newEventTypeIds.Contains(i.EventTypeId));
            var relationsToAdd = newEventTypeIds.Where(i => !existingRelations.Any(r => r.EventTypeId == i))?.Select(i => new EventTypeRelationDto
            {
                EventId = eventId,
                EventTypeId = i
            });

            if (relationsToRemove?.Count() > 0)
            {
                foreach (var relation in relationsToRemove)
                {
                    await _connection.DeleteAsync(relation);
                }
            }

            foreach (var relation in relationsToAdd)
            {
                await _connection.InsertWithIdentityAsync(relation);
            }
        }
        #endregion

        #region eventCategory
        public async Task<Guid> CreateEventCategoryAsync(EventCategoryDto item)
        {
            item.Active = true;
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task DeleteEventCategoryAsync(Guid id)
        {
            await _connection.EventCategories.Where(i => i.Id == id)
                .Set(i => i.Active, false)
                .UpdateAsync();

            // Deactivate child types together with the category.
            await _connection.EventTypes.Where(i => i.EventCategoryId == id)
                .Set(i => i.Active, false)
                .UpdateAsync();
        }

        public async Task<List<EventCategoryDto>> GetAllEventCategoriesAsync()
        {
            var result = await _connection.EventCategories
                .Where(i => i.Active)
                .ToListAsync();
            return result;
        }
        
        public async Task<EventCategoryDto?> GetEventCategoryAsync(Guid id)
        {
            var result = await _connection.EventCategories.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task UpdateEventCategoryAsync(EventCategoryDto item)
        {
            await _connection.EventCategories.Where(i => i.Id == item.Id)
                .Set(i => i.Ico, item.Ico)
                .Set(i => i.Description, item.Description)
                .Set(i => i.Name, item.Name)
                .Set(i => i.LocalizationPath, item.LocalizationPath)
                .Set(i => i.Color, item.Color)
                .UpdateAsync();
        }
        #endregion

        #region eventParameters
        public async Task<Guid> CreateEventParametersAsync(EventParametersDto item)
        {
            var result = (Guid) await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task DeleteEventParametersAsync(Guid id)
        {
            await _connection.EventParameters.DeleteAsync(i => i.Id == id);
        }

        public async Task UpdateEventParametersAsync(EventParametersDto item)
        {
            await _connection.EventParameters.Where(i => i.Id == item.Id)
                .Set(i => i.MaxPersonsCount, item.MaxPersonsCount)
                .Set(i => i.AllowedGender, item.AllowedGender)
                .Set(i => i.Cost, item.Cost)
                .Set(i => i.AgeLimit, item.AgeLimit)
                .Set(i => i.Private, item.Private)
                .Set(i => i.AllowUsersToInvite, item.AllowUsersToInvite)
                .Set(i => i.TicketsEnabled, item.TicketsEnabled)
                .UpdateAsync();
        }

        public async Task<EventParametersDto?> GetEventParametersByEventIdAsync(Guid eventId)
        {
            var result = await _connection.Events
                .LoadWith(i => i.Parameters)
                .FirstOrDefaultAsync(i => i.Id == eventId);
                
            return result?.Parameters;
        }

        public async Task<EventParametersDto?> GetEventParametersAsync(Guid id)
        {
            var result = await _connection.EventParameters.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task BindEventParametersAsync(Guid eventId, Guid eventParametersId)
        {
            var eventItem = _connection.Events.FirstOrDefault(i => i.Id == eventId);
            if (eventItem == null)
                throw new NullReferenceException($"Не удалось найти событие с id='{eventId}'");
            eventItem.EventParametersId = eventParametersId;
            await _connection.UpdateAsync(eventItem);
        }

        public async Task UpdateEventParametersCostAsync(Guid parametersId, double? cost)
        {
            await _connection.EventParameters.Where(i => i.Id == parametersId)
                .Set(i => i.Cost, cost)
                .UpdateAsync();
        }
        #endregion

        #region eventTicketTypes
        public async Task<List<EventTicketTypeDto>> GetTicketTypesByEventIdAsync(Guid eventId, bool includeInactive = false)
        {
            var q = _connection.EventTicketTypes.Where(t => t.EventId == eventId);
            if (!includeInactive)
                q = q.Where(t => t.Active);
            return await q
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.CreateDate)
                .ToListAsync();
        }

        public async Task<EventTicketTypeDto?> GetTicketTypeAsync(Guid id)
        {
            return await _connection.EventTicketTypes.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Guid> CreateTicketTypeAsync(EventTicketTypeDto item)
        {
            var now = DateTimeOffset.UtcNow;
            item.CreateDate = now;
            item.UpdateDate = now;
            if (string.IsNullOrWhiteSpace(item.Currency))
                item.Currency = "RUB";
            return (Guid)await _connection.InsertWithIdentityAsync(item);
        }

        public async Task UpdateTicketTypeAsync(EventTicketTypeDto item)
        {
            await _connection.EventTicketTypes.Where(t => t.Id == item.Id)
                .Set(t => t.Name, item.Name)
                .Set(t => t.Description, item.Description)
                .Set(t => t.Price, item.Price)
                .Set(t => t.Currency, item.Currency)
                .Set(t => t.Capacity, item.Capacity)
                .Set(t => t.SortOrder, item.SortOrder)
                .Set(t => t.Active, item.Active)
                .Set(t => t.UpdateDate, DateTimeOffset.UtcNow)
                .UpdateAsync();
        }

        public async Task DeactivateTicketTypesAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            if (idList.Count == 0)
                return;
            await _connection.EventTicketTypes.Where(t => idList.Contains(t.Id))
                .Set(t => t.Active, false)
                .Set(t => t.UpdateDate, DateTimeOffset.UtcNow)
                .UpdateAsync();
        }

        public async Task<(decimal? Min, decimal? Max)> GetActiveTicketTypePriceRangeAsync(Guid eventId)
        {
            var prices = await _connection.EventTicketTypes
                .Where(t => t.EventId == eventId && t.Active)
                .Select(t => t.Price)
                .ToListAsync();
            if (prices.Count == 0)
                return (null, null);
            return (prices.Min(), prices.Max());
        }

        public async Task<Dictionary<Guid, (decimal Min, decimal Max)>> GetActiveTicketTypePriceRangesAsync(
            IEnumerable<Guid> eventIds)
        {
            var ids = eventIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return new Dictionary<Guid, (decimal Min, decimal Max)>();

            var rows = await _connection.EventTicketTypes
                .Where(t => ids.Contains(t.EventId) && t.Active)
                .GroupBy(t => t.EventId)
                .Select(g => new
                {
                    EventId = g.Key,
                    Min = g.Min(t => t.Price),
                    Max = g.Max(t => t.Price),
                })
                .ToListAsync();

            return rows.ToDictionary(r => r.EventId, r => (r.Min, r.Max));
        }
        #endregion
    }
}
