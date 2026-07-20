namespace EList.Models.UserAgreements
{
    public class AccountAgreement
    {
        /// <summary>
        /// Идентификатор записи о соглашении
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Идентификатор аккаунта
        /// </summary>
        public Guid AccountId { get; set; }
        
        /// <summary>
        /// Идентификатор документа
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Дата соглашения
        /// </summary>
        public DateTimeOffset AgreementDate { get; set; }
    }
}
