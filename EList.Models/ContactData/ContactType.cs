namespace EList.Models.ContactData
{
    /// <summary>
    /// Тип контактных данных
    /// </summary>
    public class ContactType
    {
        /// <summary>
        /// Id Записи
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Путь до значения в файле ресурса локализации
        /// </summary>
        public string LocalizationPath { get; set; }

        /// <summary>
        /// Название типа контакта
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Описание
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

        /// <summary>
        /// Активен ли тип контакта (soft-delete)
        /// </summary>
        public bool Active { get; set; } = true;
    }
}
