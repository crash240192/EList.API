using EList.Models.Enums;

namespace EList.Models.UserAgreements
{
    public class DocumentRequest
    {
        /// <summary>
        /// Заголовок
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Текст документа
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Тип документа
        /// </summary>
        public DocumentType Type { get; set; }

        /// <summary>
        /// какой из элементов номера версии увеличивается (0.1.2)
        /// </summary>
        public string Version { get; set; }
    }
}
