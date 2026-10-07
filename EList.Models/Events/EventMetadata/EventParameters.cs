using EList.Models.Enums;

namespace EList.Models.Events.EventMetadata
{
    public class EventParameters
    {
        public Guid Id { get; set; }
        public double? Cost { get; set; }
        public bool? Private { get; set; }
        public int? MaxPersonsCount { get; set; }
        public int? AgeLimit { get; set; }
        public Gender? AllowedGender { get; set; }
        public bool? AllowUsersToInvite { get; set; }

        /// <summary>
        /// Включена ли продажа билетов на мероприятие
        /// </summary>
        public bool TicketsEnabled { get; set; }

        /// <summary>Мин. цена активных типов (derived; дублирует Cost при tickets).</summary>
        public double? PriceMin { get; set; }

        /// <summary>Макс. цена активных типов.</summary>
        public double? PriceMax { get; set; }
    }
}
