using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IAgreementDataProvider
    {
        Task<AnonymousAgeAgreementDto> GetAnonymousAgeAgreementAsync(string jwt);
        Task SaveAnonumousAgeAgreementAsync(string jwt, string clientInfo);
    }
}
