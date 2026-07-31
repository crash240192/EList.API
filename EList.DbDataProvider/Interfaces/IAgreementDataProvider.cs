using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface IAgreementDataProvider
    {
        Task<AnonymousAgeAgreementDto> GetAnonymousAgeAgreementAsync(string jwt);
        Task SaveAnonumousAgeAgreementAsync(string jwt, string clientInfo);


        Task<bool> DoesUserAgreedWithLatestDocumentVersion(Guid accountId, DocumentType documentType);
        Task SaveUserAgreementAsync(Guid accountId, Guid documentId);

        Task<bool> DoesOrganizationAgreedWithLatestDocumentVersion(Guid organizationId, DocumentType documentType);
        Task SaveOrganizationAgreementAsync(Guid organizationId, Guid documentId);

        Task AddNewDocumentAsync(DocumentDto document);
        Task<List<DocumentDto>> GetLatestDocumentsAsync();
        Task<DocumentDto> GetLatestDocumentAsync(DocumentType type);
    }
}
