using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.UserAgreements;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class AgreementRepository : IAgreementRepository
    {
        private readonly IAgreementDataProvider _agreementDataProvider;
        private readonly IMapper _mapper;
        public AgreementRepository(IAgreementDataProvider agreementDataProvider,
            IMapper mapper) 
        {
            _agreementDataProvider = agreementDataProvider;
            _mapper = mapper;
        }
        public async Task<AnonymousAgeAgreement> GetAnonymousAgeAgreementAsync(string jwt)
        {
            var item = await _agreementDataProvider.GetAnonymousAgeAgreementAsync(jwt);
            var result = _mapper.Map<AnonymousAgeAgreement>(item);
            return result;
        }

        public async Task SaveAnonymousAgeAgreement(string jwt, string clientInfo)
        {
            await _agreementDataProvider.SaveAnonumousAgeAgreementAsync(jwt, clientInfo);
        }

        #region agreements

        public async Task<bool> DoesUserAgreedWithLatestDocumentVersion(Guid accountId, Models.Enums.DocumentType documentType)
        {
            var mappedDocumentType = _mapper.Map<DbDataProvider.Models.Enums.DocumentType>(documentType);
            var result = await _agreementDataProvider.DoesUserAgreedWithLatestDocumentVersion(accountId, mappedDocumentType);
            throw new NotImplementedException();
        }

        public async Task SaveUserAgreementAsync(Guid accountId, Guid documentId)
        {
            await _agreementDataProvider.SaveUserAgreementAsync(accountId, documentId);
        }
        #endregion


        #region documents
        public async Task<List<Document>> GetLatestDocumentsAsync()
        {
            var dbItems = await _agreementDataProvider.GetLatestDocumentsAsync();
            var result = _mapper.Map<List<Document>>(dbItems);
            return result;
        }

        public async Task<Document> GetLatestDocumentAsync(Models.Enums.DocumentType type)
        {
            var mappedDocumentType = _mapper.Map<DbDataProvider.Models.Enums.DocumentType>(type);
            var dbItem = await _agreementDataProvider.GetLatestDocumentAsync(mappedDocumentType);
            var result = _mapper.Map<Document>(dbItem);
            return result;
        }

        public async Task AddNewDocumentAsync(Document document)
        {
            var mappedDocument = _mapper.Map<DocumentDto>(document);
            await _agreementDataProvider.AddNewDocumentAsync(mappedDocument);
        }
        #endregion
    }
}
