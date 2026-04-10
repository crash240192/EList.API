using EList.Models.Enums;

namespace EList.Models.Events.EventMetadata
{
    public class SetEventParametersRequest
    {
        public Guid EventId { get; set; }
        public double? Cost { get; set; }
        public bool? Private { get; set; }
        public int? MaxPersonsCount { get; set; }
        public int? AgeLimit { get; set; }
        public Gender? AllowedGender { get; set; }
    }
}
