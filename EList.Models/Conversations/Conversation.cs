using EList.Models.Events;

namespace EList.Models.Conversations
{
    public class Conversation
    {
        /// <summary>
        /// Идентификатор беседы
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// название беседы
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Ссылка на мероприятие 
        /// </summary>
        public Guid? EventId { get; set; }

        /// <summary>
        /// Диалог виден только участникам мероприятия (организаторы не ограничены)
        /// </summary>
        public bool ParticipantsOnlyVisible { get; set; }

        /// <summary>
        /// Участники могут только читать (организаторы не ограничены)
        /// </summary>
        public bool ParticipantsReadonly { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Дата обновления
        /// </summary>
        public DateTimeOffset UpdateDate { get; set; }

        /// <summary>
        /// Основная информация о событии
        /// </summary>
        public EventShort Event { get; set; }
    }
}
