namespace EList.Models.Enums
{
    /// <summary>
    /// Тип уведомления
    /// </summary>
    public enum DocumentType
    {
        /// <summary>
        /// Политика обработки ПДн
        /// </summary>
        Policy = 0,

        /// <summary>
        /// Согласие на обработку ПДн
        /// </summary>
        Consent = 1,

        /// <summary>
        /// Пользовательское соглашение
        /// </summary>
        Agreement = 2,

        /// <summary>
        /// Соглашение организации
        /// </summary>
        OrganizationAgreement = 3,

        /// <summary>
        /// Соглашение по билетам
        /// </summary>
        TicketingAgreement = 4  
    }
}
