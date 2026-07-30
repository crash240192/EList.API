namespace EList.Models.Organizations
{
    /// <summary>
    /// Запрос на добавление менеджера в организацию
    /// </summary>
    public class AddOrganizationMemberRequest
    {
        /// <summary>
        /// Идентификатор аккаунта добавляемого менеджера
        /// </summary>
        public Guid AccountId { get; set; }
    }
}
