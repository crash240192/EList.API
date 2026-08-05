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
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public Task DeleteEventTypeAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<EventTypeDto>?> GetAllEventTypesAsync()
        {
            var result = await _connection.EventTypes
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
                .Where(i => i.EventCategoryId == categoryId)
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
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public Task DeleteEventCategoryAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<EventCategoryDto>> GetAllEventCategoriesAsync()
        {
            var result = await _connection.EventCategories.ToListAsync();
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
        #endregion
    }
}
