namespace EList.Models.ContactData
{
    /// <summary>
    /// Класс-контейнер запроса на создание типа контактных данных
    /// </summary>
    public class ContactTypeRequest
    {
        /// <summary>
        /// Путь до значение в файле локализации
        /// </summary>
        public string LocalizationPath { get; set; }

        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Описание типа контакта
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Маска
        /// </summary>
        public string Mask { get; set; }

        /// <summary>
        /// Флаг доступности типа контакта для рассылки уведомлений
        /// </summary>
        public bool AllowNotifications { get; set; }
    }
}
