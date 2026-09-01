using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Person;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
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
        private readonly IPersonAccessValidator _personAccessValidator;
        private readonly IAccountDataHolder _accountDataHolder;

        public PersonService(ICorrelationIdProvider correlationIdProvider,
            IPersonsRepository personRepository,
            IPersonValidator personValidator,
            IPersonAccessValidator personAccessValidator,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
            _personValidator = personValidator ?? throw new ArgumentNullException(nameof(personValidator));
            _personAccessValidator = personAccessValidator ?? throw new ArgumentNullException(nameof(personAccessValidator));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<Guid?>> CreatePersonInfoAsync(PersonRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreatePersonInfoAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accountId = _accountDataHolder.AccountId!.Value;

            var editAccess = _personAccessValidator.CanEditPersonInfo(accountId, accountId);
            if (!editAccess.Success)
                return CommandResult<Guid?>.Fail(editAccess.ErrorCode, editAccess.Message);

            NormalizePersonRequest(request);

            var existingPersonInfo = await _personRepository.GetPersonInfoAsync(accountId);

            var validationResult = existingPersonInfo == null
                ? _personValidator.ValidateCreation(request)
                : await _personValidator.ValidateUpdation(accountId, request);

            if (!validationResult.Success)
                return CommandResult<Guid?>.Fail(validationResult.ErrorCode, validationResult.Message);

            Guid result;
            if (existingPersonInfo == null)
            {
                result = await _personRepository.CreatePersonInfoAsync(accountId, request);
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

            var accessResult = await _personAccessValidator.CanViewPersonInfoAsync(accountId, _accountDataHolder.AccountId);
            if (!accessResult.Success)
                return CommandResult<PersonInfo?>.Fail(accessResult.ErrorCode, accessResult.Message);

            var result = await _personRepository.GetPersonInfoAsync(accountId);

            if (result == null)
                return CommandResult<PersonInfo?>.Fail(ErrorCode.AccountNotFound, "Персональные данные аккаунта не найдены");

            result = _personAccessValidator.ApplyViewPolicy(result, accountId, _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PersonInfo?>(result);
        }

        public async Task<CommandResult<PersonInfo?>> GetPersonInfoAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetPersonInfoAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accountId = _accountDataHolder.AccountId!.Value;
            var result = await _personRepository.GetPersonInfoAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PersonInfo?>(result);
        }

        private static void NormalizePersonRequest(PersonRequest request)
        {
            request.FirstName = request.FirstName?.Trim();
            request.LastName = request.LastName?.Trim();
            request.Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim();
        }
    }
}
