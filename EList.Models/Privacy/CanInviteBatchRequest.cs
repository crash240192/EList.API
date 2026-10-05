namespace EList.Models.Privacy
{
    /// <summary>
    /// Тело POST /api/accounts/canInvite/batch.
    /// </summary>
    public class CanInviteBatchRequest
    {
        public List<Guid> AccountIds { get; set; } = new();
    }
}
