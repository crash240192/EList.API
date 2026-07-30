namespace EList.Models.Organizations
{
    /// <summary>
    /// Запрос на передачу владения организацией
    /// </summary>
    public class TransferOwnershipRequest
    {
        /// <summary>
        /// Идентификатор аккаунта нового владельца
        /// </summary>
        public Guid NewOwnerAccountId { get; set; }
    }
}
