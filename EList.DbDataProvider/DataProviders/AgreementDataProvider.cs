using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class AgreementDataProvider : DataProviderBase, IAgreementDataProvider
    {
        public AgreementDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }


        #region anonymous agreements
        public async Task<AnonymousAgeAgreementDto> GetAnonymousAgeAgreementAsync(string jwt)
        {
            var threshold = DateTimeOffset.UtcNow.AddHours(-1);
            var item = await _connection.AnonymousAgeAgreements.FirstOrDefaultAsync(i => i.Jwt == jwt && i.AgreementDate >= threshold);
            return item;
        }

        public async Task SaveAnonumousAgeAgreementAsync(string jwt, string clientInfo)
        {
            await _connection.InsertWithIdentityAsync(new AnonymousAgeAgreementDto
            {
                Jwt = jwt,
                ClientInfo = clientInfo,
                AgreementDate = DateTime.UtcNow
            });
        }
        #endregion



        #region documents
        public async Task AddNewDocumentAsync(DocumentDto document)
        {
            document.CreationDate = DateTime.UtcNow;
            await _connection.InsertWithIdentityAsync(document);
        }

        public async Task<List<DocumentDto>> GetLatestDocumentsAsync()
        {
            var result = new List<DocumentDto>();
            var agreement = await _connection.Documents.Where(i => i.Type == DocumentType.Agreement)
                .OrderByDescending(i => i.CreationDate)
                .FirstOrDefaultAsync();
            if (agreement != null)
                result.Add(agreement);

            var consent = await _connection.Documents.Where(i => i.Type == DocumentType.Consent)
                .OrderByDescending(i => i.CreationDate)
                .FirstOrDefaultAsync();
            if (consent != null) 
                result.Add(consent);

            var policy = await _connection.Documents.Where(i => i.Type == DocumentType.Policy)
                .OrderByDescending(i => i.CreationDate)
                .FirstOrDefaultAsync();
            if (policy != null) 
                result.Add(policy);

            return result;
        }

        public async Task<DocumentDto> GetLatestDocumentAsync(DocumentType type)
        {
            var document = await _connection.Documents.Where(i => i.Type == type)
                .OrderByDescending(i => i.CreationDate)
                .FirstOrDefaultAsync();
            return document;
        }
        #endregion



        #region user agreements
        public async Task<bool> DoesUserAgreedWithLatestDocumentVersion(Guid accountId, DocumentType documentType)
        {
            var documentId = await _connection.Documents.Where(i => i.Type == documentType)
                .OrderByDescending(i => i.CreationDate)
                .Select(i => i.Id)
                .FirstOrDefaultAsync();

            var userAgreed = await _connection.Agreements.AnyAsync(i => i.AccountId == accountId && i.DocumentId == documentId);

            return userAgreed;
        }

        public async Task SaveUserAgreementAsync(Guid accountId, Guid documentId)
        {
            await _connection.InsertWithIdentityAsync(new AccountAgreementDto
            {
                AccountId = accountId,
                AgreementDate = DateTimeOffset.UtcNow,
                DocumentId = documentId,
            });
            throw new NotImplementedException();
        }
        #endregion
    }
}
