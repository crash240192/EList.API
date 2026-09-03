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
            var hours = 1;
            if (EList.Common.Configuration.ConfigurationManager.AppSettings.Contains("agreements:anonymousAgeTtlHours")
                && int.TryParse(
                    EList.Common.Configuration.ConfigurationManager.AppSettings["agreements:anonymousAgeTtlHours"],
                    out var configuredHours)
                && configuredHours > 0)
            {
                hours = configuredHours;
            }

            var threshold = DateTimeOffset.UtcNow.AddHours(-hours);
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
            var documentTypes = new[]
            {
                DocumentType.Agreement,
                DocumentType.Consent,
                DocumentType.Policy,
                DocumentType.OrganizationAgreement,
                DocumentType.TicketingAgreement
            };

            foreach (var documentType in documentTypes)
            {
                var document = await _connection.Documents.Where(i => i.Type == documentType)
                    .OrderByDescending(i => i.CreationDate)
                    .FirstOrDefaultAsync();
                if (document != null)
                    result.Add(document);
            }

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
        }
        #endregion



        #region organization agreements
        public async Task<bool> DoesOrganizationAgreedWithLatestDocumentVersion(Guid organizationId, DocumentType documentType)
        {
            var documentId = await _connection.Documents.Where(i => i.Type == documentType)
                .OrderByDescending(i => i.CreationDate)
                .Select(i => i.Id)
                .FirstOrDefaultAsync();

            var organizationAgreed = await _connection.OrganizationAgreements
                .AnyAsync(i => i.OrganizationId == organizationId && i.DocumentId == documentId);

            return organizationAgreed;
        }

        public async Task SaveOrganizationAgreementAsync(Guid organizationId, Guid documentId)
        {
            await _connection.InsertWithIdentityAsync(new OrganizationAgreementDto
            {
                OrganizationId = organizationId,
                AgreementDate = DateTimeOffset.UtcNow,
                DocumentId = documentId,
            });
        }
        #endregion
    }
}
