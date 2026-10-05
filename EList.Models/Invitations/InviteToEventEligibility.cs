namespace EList.Models.Invitations
{
    /// <summary>
    /// Результат проверки «можно ли пригласить аккаунт на мероприятие».
    /// </summary>
    public class InviteToEventEligibility
    {
        public Guid EventId { get; set; }

        public Guid AccountId { get; set; }

        public bool Allowed { get; set; }

        public string? Reason { get; set; }

        public int ErrorCode { get; set; }

        /// <summary>
        /// Информативно: приглашение не даёт бесплатный вход (нужен билет).
        /// Не блокирует отправку приглашения.
        /// </summary>
        public bool TicketsRequired { get; set; }
    }
}
