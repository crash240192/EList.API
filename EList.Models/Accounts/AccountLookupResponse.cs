namespace EList.Models.Accounts
{
    /// <summary>
    /// Публичный lookup аккаунта по логину или id (для gift/transfer).
    /// </summary>
    public class AccountLookupResponse
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public Guid? AvatarId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
