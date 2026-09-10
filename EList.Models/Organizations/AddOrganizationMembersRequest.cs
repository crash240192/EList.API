namespace EList.Models.Organizations
{
    /// <summary>
    /// Запрос на добавление нескольких менеджеров в организацию
    /// </summary>
    public class AddOrganizationMembersRequest
    {
        /// <summary>
        /// Идентификаторы аккаунтов добавляемых менеджеров
        /// </summary>
        public List<Guid> AccountIds { get; set; } = new();
    }
}
