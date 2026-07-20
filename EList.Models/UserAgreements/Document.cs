using EList.Models.Enums;

namespace EList.Models.UserAgreements
{
    public class Document
    {
        /// <summary>
        /// Идентификатор документа
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Заголовок
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Текст документа
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Хэш документа
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Тип документа
        /// </summary>
        public DocumentType Type { get; set; }

        /// <summary>
        /// Версия документа
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTimeOffset CreationDate { get; set; }
    }
}
