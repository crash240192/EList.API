using EList.Common.Models;
using EList.Models.UserAgreements;

namespace EList.Services.Interfaces
{
    public interface IAgreementService
    {
        Task<CommandResult<AnonymousAgeAgreement>> GetAnonymousAgeAgreementAsync();
        Task<CommandResult> SaveAnonymousAgeAgreementAsync();
    }
}
