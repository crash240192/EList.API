namespace EList.Models.Conversations
{
    public class MessageRequest
    {
        /// <summary>
        /// Идентификатор сообщения (пусто при создании)
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Ссылка на беседу
        /// </summary>
        public Guid ConversationId { get; set; }

        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string MessageText { get; set; }

        /// <summary>
        /// Ссылка на аккаунт отправителя сообщения
        /// </summary>
        public Guid? AccountId { get; set; }

        /// <summary>
        /// Ссылка на организацию отправителя сообщения
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Ответ на указанное сообщение
        /// </summary>
        public Guid? ReplyTo { get; set; }
    }
}
