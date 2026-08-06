using EList.Models.Events;

namespace EList.Models.EventTemplates
{
    public class EventTemplate
    {
        public Guid Id { get; set; }

        public Guid? OwnerAccountId { get; set; }

        public Guid? OwnerOrganizationId { get; set; }

        public string Name { get; set; }

        public CreateEventRequest TemplateBody { get; set; }

        public DateTimeOffset CreateDate { get; set; }

        public DateTimeOffset UpdateDate { get; set; }
    }
}
