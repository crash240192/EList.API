using EList.Models.Enums;
using EList.Models.UserAgreements;

namespace EList.Repositories.Interfaces
{
    public interface IAgreementRepository
    {
        Task<AnonymousAgeAgreement> GetAnonymousAgeAgreementAsync(string jwt);
        Task SaveAnonymousAgeAgreement(string jwt, string clientInfo);

        Task<bool> DoesUserAgreedWithLatestDocumentVersion(Guid accountId, DocumentType documentType);
        Task SaveUserAgreementAsync(Guid accountId, Guid documentId);

        Task<bool> DoesOrganizationAgreedWithLatestDocumentVersion(Guid organizationId, DocumentType documentType);
        Task SaveOrganizationAgreementAsync(Guid organizationId, Guid documentId);

        Task AddNewDocumentAsync(Document document);
        Task<List<Document>> GetLatestDocumentsAsync();
        Task<Document> GetLatestDocumentAsync(DocumentType type);
    }
}
