namespace EList.Models.UserAgreements
{
    public class AnonymousAgeAgreement
    {
        /// <summary>
        /// Идентификатор записи
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Jwt клиента
        /// </summary>
        public string Jwt { get; set; }

        /// <summary>
        /// Дата соглашения
        /// </summary>
        public DateTimeOffset AgreementDate { get; set; }

        /// <summary>
        /// Информация о клиенте
        /// </summary>
        public string ClientInfo { get; set; }
    }
}
