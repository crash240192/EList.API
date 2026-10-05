namespace EList.Models.Privacy
{
    /// <summary>
    /// Ответ GET /api/accounts/canInvite/{accountId}.
    /// </summary>
    public class CanInviteResult
    {
        public bool Allowed { get; set; }

        /// <summary>Понятная причина отказа для UI (tooltip / disabled).</summary>
        public string? Reason { get; set; }
    }
}
