namespace EList.Models.Privacy
{
    /// <summary>
    /// Ответ GET /api/accounts/canInvite/{accountId} и элемент batch.
    /// </summary>
    public class CanInviteResult
    {
        /// <summary>Целевой аккаунт (заполняется в batch).</summary>
        public Guid? AccountId { get; set; }

        public bool Allowed { get; set; }

        /// <summary>Понятная причина отказа для UI (tooltip / disabled).</summary>
        public string? Reason { get; set; }
    }
}
