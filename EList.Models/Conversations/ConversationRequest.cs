namespace EList.Models.Conversations
{
    public class ConversationRequest
    {
        /// <summary>
        /// Идентификатор беседы (пусто при создании)
        /// </summary>
        public Guid? Id { get; set; }
        
        /// <summary>
        /// название беседы
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Ссылка на мероприятие 
        /// </summary>
        public Guid? EventId { get; set; }
    }
}
