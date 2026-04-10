using EList.Models.Enums;

namespace EList.Models.Events.EventMetadata
{
    public class EventParametersRequest
    {
        public double? Cost { get; set; }
        public bool? Private { get; set; }
        public int? MaxPersonsCount { get; set; }
        public int? AgeLimit { get; set; }
        public Gender? AllowedGender { get; set; }
        public bool? AllowUsersToInvite { get; set; }
    }
}
