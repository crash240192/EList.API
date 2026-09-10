namespace EList.Models.Organizations
{
    /// <summary>
    /// Результат пакетного добавления менеджеров
    /// </summary>
    public class AddOrganizationMembersResponse
    {
        /// <summary>
        /// Аккаунты, которые стали менеджерами (новые или повторно активированные)
        /// </summary>
        public List<Guid> AddedAccountIds { get; set; } = new();

        /// <summary>
        /// Уже активные участники организации — пропущены
        /// </summary>
        public List<Guid> AlreadyMembers { get; set; } = new();

        /// <summary>
        /// Аккаунты, которые не найдены — пропущены
        /// </summary>
        public List<Guid> NotFound { get; set; } = new();
    }
}
