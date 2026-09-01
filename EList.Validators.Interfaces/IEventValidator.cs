using EList.Common.Models;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;

namespace EList.Validators.Interfaces
{
    public interface IEventValidator
    {
        CommandResult ValidateEventBody(EventRequest? request, bool requireName = true);

        CommandResult ValidateParameters(EventParametersRequest? parameters);

        CommandResult ValidateCreateRequest(CreateEventRequest? request);

        CommandResult ValidateEventTypeIds(List<Guid>? typeIds, bool requireAtLeastOne = false);

        CommandResult ValidateSearchRequest(EventsSearchRequest? request);
    }
}
