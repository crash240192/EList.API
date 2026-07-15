using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.UserAgreements;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl
{
    public class AgreementService : IAgreementService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.AgreementService.";
        #endregion

        private readonly IAgreementRepository _agreementRepository;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountDataHolder _accountDataHolder;
        public AgreementService(ICorrelationIdProvider correlationIdProvider,
            IAgreementRepository agreementRepository,
            IAccountDataHolder accountDataHolder) 
        {
            _agreementRepository = agreementRepository;
            _correlationIdProvider = correlationIdProvider;
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<AnonymousAgeAgreement>> GetAnonymousAgeAgreementAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAnonymousAgeAgreementAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var agreement = await _agreementRepository.GetAnonymousAgeAgreementAsync(_accountDataHolder.Jwt);
            
            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            if (agreement != null) 
                return new CommandResult<AnonymousAgeAgreement>(agreement);
            else
                return CommandResult<AnonymousAgeAgreement>.Fail(ErrorCode.AgreementNotFound, "Пользователь не подтвердил что ему есть 18");
        }

        public async Task<CommandResult> SaveAnonymousAgeAgreementAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SaveAnonymousAgeAgreementAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            await _agreementRepository.SaveAnonymousAgeAgreement(_accountDataHolder.Jwt, _accountDataHolder.ClientInfo);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
