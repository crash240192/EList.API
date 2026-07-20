using EList.Common.Models;
using EList.Models.Enums;
using EList.Models.UserAgreements;

namespace EList.Services.Interfaces
{
    public interface IAgreementService
    {
        Task<CommandResult<AnonymousAgeAgreement>> GetAnonymousAgeAgreementAsync();
        Task<CommandResult> SaveAnonymousAgeAgreementAsync();


        Task<CommandResult> DoesUserAgreedWithLatestDocumentVersion(DocumentType documentType);
        Task<CommandResult> SaveUserAgreementAsync(DocumentType documentType);


        Task<CommandResult> AddNewDocumentAsync(DocumentRequest request);
        Task<CommandResult<List<Document>>> GetLatestDocumentsAsync();
        Task<CommandResult<Document>> GetLatestDocumentAsync(DocumentType documentType);
    }
}
