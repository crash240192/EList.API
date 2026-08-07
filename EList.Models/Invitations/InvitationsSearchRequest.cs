namespace EList.Models.Invitations
{
    /// <summary>
    /// Тело запроса поиска приглашений
    /// </summary>
    public class InvitationsSearchRequest
    {
        /// <summary>
        /// Список идентификаторов пригласителей (аккаунтов).
        /// Также включает приглашения от организаций, в которых эти аккаунты состоят.
        /// </summary>
        public List<Guid>? InviterAccountIds { get; set; }

        /// <summary>
        /// Список идентификаторов приглашённых пользователей
        /// </summary>
        public List<Guid>? InvitedAccountIds { get; set; }

        /// <summary>
        /// Список идентификаторов организаций-пригласителей
        /// </summary>
        public List<Guid>? InviterOrgIds { get; set; }

        /// <summary>
        /// Список идентификаторов событий
        /// </summary>
        public List<Guid>? EventIds { get; set; }

        /// <summary>
        /// Флаг просмотренных приглашений
        /// </summary>
        public bool? Viewed { get; set; }

        /// <summary>
        /// Размер страницы
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Номер страницы
        /// </summary>
        public int? PageIndex { get; set; }
    }
}
