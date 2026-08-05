using EList.Models.Enums;

namespace EList.Models.Events.EventMetadata
{
    public class EventParametersRequest
    {
        public double? Cost { get; set; }
        public bool? Private { get; set; }
        public int? MaxPersonsCount { get; set; }
        public int AgeLimit { get; set; }
        public Gender? AllowedGender { get; set; }
        public bool? AllowUsersToInvite { get; set; }

        /// <summary>
        /// Включена ли продажа билетов на мероприятие.
        /// Требует организацию-организатора с can_sell_tickets = true.
        /// </summary>
        public bool TicketsEnabled { get; set; } = false;
    }
}
