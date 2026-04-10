using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Accounts;
using EList.Models.Person;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using Newtonsoft.Json.Linq;
using NLog;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class PersonService : IPersonsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.PersonService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IPersonsRepository _personRepository;
        private readonly IPersonValidator _personValidator;
        private readonly IAuthorizationRepository _authorizationRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IMediaRepository _mediaRepository;

        public PersonService(ICorrelationIdProvider correlationIdProvider,
            IPersonsRepository personRepository,
            IPersonValidator personValidator,
            IAuthorizationRepository authorizationRepository,
            IAccountDataHolder accountDataHolder,
            IMediaRepository mediaRepository)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
            _personValidator = personValidator ?? throw new ArgumentNullException(nameof(personValidator));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _accountDataHolder = accountDataHolder;
            _mediaRepository = mediaRepository;
        }

        public async Task<CommandResult<Guid?>> CreatePersonInfoAsync(PersonRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreatePersonInfoAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Validate person info
            //var validationResult = _personValidator.ValidateCreation(request);
            //if (!validationResult.Success)
            //    return CommandResult<Guid>.Fail(validationResult.ErrorCode, validationResult.Message);

            var existingPersonInfo = await _personRepository.GetPersonInfoAsync(_accountDataHolder.AccountId);

            Guid result;
            if (existingPersonInfo == null)
            {
                result = await _personRepository.CreatePersonInfoAsync(_accountDataHolder.AccountId, request);
            }
            else
            {
                await _personRepository.UpdatePersonInfoAsync(existingPersonInfo.AccountId, request);
                result = existingPersonInfo.Id;
            }
            
            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult<PersonInfo?>> GetPersonInfoByAccountIdAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetPersonInfoByAccountIdAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _personRepository.GetPersonInfoAsync(accountId);

            if (result == null)
                return CommandResult<PersonInfo?>.Fail(ErrorCode.AccountNotFound, "Персональные данные аккаунта не найдены");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PersonInfo?>(result);
        }

        public async Task<CommandResult<PersonInfo?>> GetPersonInfoByTokenAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetPersonInfoByAccountIdAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _personRepository.GetPersonInfoAsync(_accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PersonInfo?>(result);
        }


        //public async Task<CommandResult> UpdatePersonInfoAsync(Guid token, PersonRequest request)
        //{
        //    var correlationId = _correlationIdProvider.Get();
        //    var execTime = Stopwatch.StartNew();
        //    var methodName = $"{LOGGER_NAME}{nameof(UpdatePersonInfoAsync)}";

        //    logger.Debug(correlationId, null, methodName, $"Method started", null);

        //    var tokenInfo = await _authorizationRepository.GetAuthorizationDataAsync(token);

        //    //var validationResult = await _personValidator.ValidateUpdation(accountId, request);
        //    //if (!validationResult.Success)
        //    //    return validationResult;

        //    await _personRepository.UpdatePersonInfoAsync(tokenInfo.AccountId, request);

        //    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
        //    return CommandResult.OK;
        //}
    }
}