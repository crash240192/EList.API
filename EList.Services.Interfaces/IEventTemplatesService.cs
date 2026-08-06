using EList.Common.Models;
using EList.Models.EventTemplates;

namespace EList.Services.Interfaces
{
    public interface IEventTemplatesService
    {
        Task<CommandResult<Guid?>> CreateTemplateAsync(CreateEventTemplateRequest request);

        Task<CommandResult<EventTemplateResponse?>> GetTemplateAsync(Guid templateId);

        Task<CommandResult> UpdateTemplateAsync(Guid templateId, UpdateEventTemplateRequest request);

        Task<CommandResult> DeleteTemplateAsync(Guid templateId);

        Task<CommandResult<List<EventTemplateResponse>>> SearchTemplatesAsync(EventTemplateSearchRequest request);
    }
}
