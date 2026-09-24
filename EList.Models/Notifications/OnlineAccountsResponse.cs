namespace EList.Models.Notifications
{
    /// <summary>
    /// Ответ: какие из запрошенных аккаунтов сейчас онлайн (WS)
    /// </summary>
    public class OnlineAccountsResponse
    {
        public List<Guid> OnlineAccountIds { get; set; } = new();
    }
}
