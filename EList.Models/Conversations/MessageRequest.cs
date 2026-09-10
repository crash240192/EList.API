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
        /// Сообщение, на которое нажали «Ответить» (любой комментарий в треде).
        /// Сервер сохраняет <c>reply_to</c> на корневой комментарий, чтобы вложенность была только из двух уровней.
        /// Текст <see cref="MessageText"/> сохраняется как есть: чтобы адресовать автора ответа,
        /// клиент может вставить <c>suggestedReplyPrefix</c> этого сообщения в начало текста.
        /// </summary>
        public Guid? ReplyTo { get; set; }
    }
}
