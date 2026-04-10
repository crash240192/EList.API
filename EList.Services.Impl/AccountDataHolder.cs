using EList.Services.Interfaces;

namespace EList.Services.Impl
{
    public class AccountDataHolder : IAccountDataHolder
    {
        public Guid Token { get; set;}
        public Guid AccountId { get; set;}
    }
}
