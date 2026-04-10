namespace EList.Services.Interfaces
{
    public interface IAccountDataHolder
    {
        Guid Token { get; set; }
        Guid AccountId { get; set; }
    }
}
