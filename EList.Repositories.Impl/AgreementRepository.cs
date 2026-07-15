using AutoMapper;
using EList.DbDataProvider.Interfaces;
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
    }
}
