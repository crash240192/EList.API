using EList.Models.Accounts;
using EList.Models.Person;

namespace EList.Models.Conversations
{
    public class Message
    {
        /// <summary>
        /// Идентификатор сообщения
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Ссылка на беседу
        /// </summary>
        public Guid ConversationId { get; set; }

        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string MessageText { get; set; }

        /// <summary>
        /// На это сообщение есть ответы
        /// </summary>
        public bool Replied { get; set; }

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

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Дата обновления
        /// </summary>
        public DateTimeOffset UpdateDate { get; set; }

        /// <summary>
        /// Сообщение скрыто модерацией
        /// </summary>
        public bool Hidden { get; set; }

        public DateTimeOffset? HiddenAt { get; set; }
        public Guid? HiddenBy { get; set; }


        public AccountPublicData Account { get; set; }  
        public PersonInfo PersonInfo { get; set; }
    }
}
