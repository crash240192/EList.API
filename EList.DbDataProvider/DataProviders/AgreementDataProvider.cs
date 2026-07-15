using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class AgreementDataProvider : DataProviderBase, IAgreementDataProvider
    {
        public AgreementDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<AnonymousAgeAgreementDto> GetAnonymousAgeAgreementAsync(string jwt)
        {
            var item = await _connection.AnonymousAgeAgreements.FirstOrDefaultAsync(i => i.Jwt == jwt);
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
    }
}
