using EList.Models.UserAgreements;

namespace EList.Repositories.Interfaces
{
    public interface IAgreementRepository
    {
        Task<AnonymousAgeAgreement> GetAnonymousAgeAgreementAsync(string jwt);
        Task SaveAnonymousAgeAgreement(string jwt, string clientInfo);
    }
}
