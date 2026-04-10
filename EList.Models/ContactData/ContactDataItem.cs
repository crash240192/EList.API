namespace EList.Models.ContactData
{
    /// <summary>
    /// Контактные данные
    /// </summary>
    public class ContactDataItem
    {
        /// <summary>
        /// Id Записи
        /// </summary>
        public Guid Id { get; set; }

        public bool IsAuthorizationContact { get; set; }

        /// <summary>
        /// Флаг разрешения отображения контакта другим пользователям
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// Значение
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Тип контакта
        /// </summary>
        public ContactType ContactType { get; set; }
        
        /// <summary>
        /// Идентификатор аккаунта-обладателя 
        /// </summary>
        public Guid? AccountId { get; set; }

        /// <summary>
        /// Идентификатор организации-обладателя
        /// </summary>
        public Guid? OrganizationId { get; set; }
    }
}
