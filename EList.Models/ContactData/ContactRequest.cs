namespace EList.Models.ContactData
{
    /// <summary>
    /// Класс-контейнер создания записи контактных данных
    /// </summary>
    public class ContactRequest
    {
        /// <summary>
        /// Id типа контакта
        /// </summary>
        public Guid TypeId { get; set; }

        /// <summary>
        /// Значение контакта
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Флаг использования контакта для авторизации
        /// </summary>
        public bool IsAuthorizationContact { get; set; }

        /// <summary>
        /// Флаг разрешения отображения контакта другим пользователям
        /// </summary>
        public bool Show { get; set; }
    }
}
