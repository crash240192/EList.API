using AutoMapper;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.Wallets;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class WalletsService : IWalletsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.WalletsService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IWalletsRepository _walletsRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IPaymentProvider _paymentProvider;
        private readonly IMapper _mapper;

        public WalletsService(ICorrelationIdProvider correlationIdProvider,
            IWalletsRepository walletsRepository,
            IAccountsRepository accountsRepository,
            IOrganizationsRepository organizationsRepository,
            IAccountDataHolder accountDataHolder,
            IPaymentProvider paymentProvider,
            IMapper mapper)
        {
            _correlationIdProvider = correlationIdProvider;
            _walletsRepository = walletsRepository;
            _accountsRepository = accountsRepository;
            _organizationsRepository = organizationsRepository;
            _accountDataHolder = accountDataHolder;
            _paymentProvider = paymentProvider;
            _mapper = mapper;
        }


        public async Task<CommandResult<Guid?>> CreateTariffAsync(Tariff item)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateTariffAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var validator = await _walletsRepository.GetTariffValidatorAsync(item.ValidatorId);
            if (validator == null)
                return CommandResult<Guid?>.Fail(ErrorCode.TariffValidatorNotFound, $"Валидатор тарифа с id='{item.ValidatorId}' не найден");

            var result = await _walletsRepository.CreateTariffAsync(item);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult> UpdateTariffAsync(Tariff item)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateTariffAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var existingTariff = await _walletsRepository.GetTariffAsync(item.Id);
            if (existingTariff == null)
                return CommandResult.Fail(ErrorCode.TariffNotFound, $"Тариф с id='{item.Id}' не найден");

            var validator = await _walletsRepository.GetTariffValidatorAsync(item.ValidatorId);
            if (validator == null)
                return CommandResult<Guid?>.Fail(ErrorCode.TariffValidatorNotFound, $"Валидатор тарифа с id='{item.ValidatorId}' не найден");

            await _walletsRepository.UpdateTariffAsync(item);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<Tariff?>> GetTariffAsync(Guid tariffId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateTariffAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _walletsRepository.GetTariffAsync(tariffId);

            if (result == null)
                return CommandResult<Tariff?>.Fail(ErrorCode.TariffNotFound, $"Тариф с id='{tariffId}' не найден");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Tariff?>(result);
        }


        public async Task<CommandResult<Guid?>> CreateTariffValidatorAsync(TariffValidator item)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateTariffValidatorAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            item.AgeLimit ??= 0;

            var result = await _walletsRepository.CreateTariffValidatorAsync(item);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult> UpdateTariffValidatorAsync(TariffValidator item)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateTariffValidatorAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var tariffValidator = await _walletsRepository.GetTariffValidatorAsync(item.Id);
            if (tariffValidator == null)
                return CommandResult.Fail(ErrorCode.TariffValidatorNotFound, $"Валидатор тарифа с id='{item.Id}' не найден");

            item.AgeLimit ??= 0;

            await _walletsRepository.UpdateTariffValidatorAsync(item);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<TariffValidator?>> GetTariffValidatorAsync(Guid tariffValidatorId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetTariffValidatorAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _walletsRepository.GetTariffValidatorAsync(tariffValidatorId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<TariffValidator?>(result);
        }

        public async Task<CommandResult<TariffValidator?>> GetTariffValidatorByTariffIdAsync(Guid tariffId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetTariffValidatorByTariffIdAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var tariff = await _walletsRepository.GetTariffAsync(tariffId);
            if (tariff == null)
                return CommandResult<TariffValidator?>.Fail(ErrorCode.TariffNotFound, $"Тариф с id='{tariffId}' не найден");

            var result = await _walletsRepository.GetTariffValidatorByTariffIdAsync(tariffId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<TariffValidator?>(result);
        }


        public async Task<CommandResult<Guid?>> CreateAccountWalletAsync(Guid? accountId = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAccountWalletAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var account = await _accountsRepository.GetAccountAsync(accountId ?? _accountDataHolder.AccountId.Value);

            if (account.WalletId == null)
            {
                var result = await _walletsRepository.CreateWalletAsync();
                await _accountsRepository.SetAccountWalletAsync(_accountDataHolder.AccountId.Value, result);
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return new CommandResult<Guid?>(result);
            }
            else
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                var result = new CommandResult<Guid?>(account.WalletId);
                result.Message = $"Для текущего аккаунта уже существует кошелёк";
                return result;
            }
        }

        public async Task<CommandResult<Guid?>> CreateOrganizationWalletAsync(Wallet item)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateOrganizationWalletAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (item?.Id == null || item.Id == Guid.Empty)
                return CommandResult<Guid?>.Fail(ErrorCode.IsNullOrEmpty, "Не указан идентификатор организации в поле Id");

            var organizationId = item.Id;
            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult<Guid?>.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            if (_accountDataHolder.AccountId == null
                || !await _organizationsRepository.IsOwnerAsync(organizationId, _accountDataHolder.AccountId.Value))
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Действие доступно только владельцу организации");

            if (organization.WalletId != null)
            {
                var existing = new CommandResult<Guid?>(organization.WalletId);
                existing.Message = "Для текущей организации уже существует кошелёк";
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return existing;
            }

            var walletId = await _walletsRepository.CreateWalletAsync();
            await _organizationsRepository.SetOrganizationWalletAsync(organizationId, walletId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(walletId);
        }

        public async Task<CommandResult> SetWalletTariffAsync(Guid walletId, Guid tariffId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetWalletTariffAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            var access = await AssertCanManageWalletAsync(walletId);
            if (!access.Success)
                return access;

            var tariff = await _walletsRepository.GetTariffAsync(tariffId);
            if (tariff == null)
                return CommandResult.Fail(ErrorCode.TariffNotFound, $"Тариф с id='{tariffId}' не найден");

            var wallet = await _walletsRepository.GetWalletAsync(walletId);
            if (wallet == null)
                return CommandResult.Fail(ErrorCode.WalletNotFound, $"Кошелёк с id='{walletId}' не найден");

            var orgId = await _walletsRepository.FindOrganizationIdByWalletAsync(walletId);
            if (tariff.ForOrganization && orgId == null)
            {
                return CommandResult.Fail(ErrorCode.InvalidValue,
                    "Этот тариф предназначен для организации");
            }
            if (!tariff.ForOrganization && orgId != null)
            {
                return CommandResult.Fail(ErrorCode.InvalidValue,
                    "Этот тариф предназначен для личного аккаунта");
            }

            await _walletsRepository.SetWalletTariffAsync(walletId, tariffId);
            // Смена тарифа: текущий период не переносим — пробуем списать новый сразу.
            await _walletsRepository.ClearNextChargeAtAsync(walletId);
            var charged = await _walletsRepository.ChargeByTariffAsync(walletId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            if (!charged && tariff.Cost > 0)
            {
                var pending = CommandResult.OK;
                pending.Message = "Тариф выбран. Для активации периода пополните баланс до суммы тарифа.";
                return pending;
            }
            return CommandResult.OK;
        }

        public async Task<CommandResult<Wallet?>> GetWalletAsync(Guid walletId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetWalletAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _walletsRepository.GetWalletAsync(walletId);
            if (result == null)
                return CommandResult<Wallet?>.Fail(ErrorCode.WalletNotFound, $"Кошелёк с id='{walletId}' не найден");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Wallet?>(result);
        }

        public async Task<CommandResult<Wallet?>> GetAccountWalletAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountWalletAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var account = await _accountsRepository.GetAccountAsync(accountId);
            if (account == null)
                return CommandResult<Wallet?>.Fail(ErrorCode.AccountNotFound, $"Аккаунт с id='{accountId}' не найден");

            var result = await _walletsRepository.GetAccountWalletAsync(accountId);
            if (result == null)
                return CommandResult<Wallet?>.Fail(ErrorCode.WalletNotFound, $"Кошелёк для аккаунта с id='{accountId}' не найден");

            if (accountId != _accountDataHolder.AccountId)
                return CommandResult<Wallet?>.Fail(ErrorCode.AccessError, $"Это чужой кошелёк, нечего сюда смотреть");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Wallet?>(result);
        }


        public async Task<CommandResult<Wallet?>> GetOrganizationWalletAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizationWalletAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult<Wallet?>.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            if (_accountDataHolder.AccountId == null
                || !await _organizationsRepository.IsOwnerOrManagerAsync(organizationId, _accountDataHolder.AccountId.Value))
                return CommandResult<Wallet?>.Fail(ErrorCode.AccessError, "Действие доступно только владельцу или менеджеру организации");

            var result = await _walletsRepository.GetOrganizationWalletAsync(organizationId);
            if (result == null)
                return CommandResult<Wallet?>.Fail(ErrorCode.WalletNotFound, $"Кошелёк для организации с id='{organizationId}' не найден");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Wallet?>(result);
        }

        public async Task<CommandResult<Tariff?>> GetWalletTariffAsync(Guid walletId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetWalletTariffAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var wallet = await _walletsRepository.GetWalletAsync(walletId);
            if (wallet == null)
                return CommandResult<Tariff?>.Fail(ErrorCode.WalletNotFound, $"Кошелёк с id='{walletId}' не найден");

            if (wallet.TariffId == null)
                return CommandResult<Tariff?>.Fail(ErrorCode.TariffNotAssigned, $"Тариф для кошелька с id='{walletId}' не назначен");

            var result = await _walletsRepository.GetWalletTariffAsync(walletId);
            if (result == null)
                return CommandResult<Tariff?>.Fail(ErrorCode.WalletNotFound, $"Тариф для кошелька с id='{walletId}' не найден");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Tariff?>(result);
        }

        public async Task<CommandResult<List<Tariff>?>> GetTariffsAsync(bool? forOrganization = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetTariffsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _walletsRepository.GetTariffsAsync(forOrganization);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<Tariff>?>(result);
        }

        public async Task<CommandResult<List<Wallet>>> GetOverdueWalletsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOverdueWalletsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _walletsRepository.GetOverdueWalletsAsync();

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<Wallet>>(result);
        }

        public async Task<CommandResult> DepositeAsync(Guid walletId, double value)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DepositeAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (value <= 0)
                return CommandResult.Fail(ErrorCode.PaymentValueMustBeOverZero, "Значение зачисляемых средств должно быть больше нуля");

            var wallet = await _walletsRepository.GetWalletAsync(walletId);
            if (wallet == null)
                return CommandResult.Fail(ErrorCode.WalletNotFound, $"Кошелёк с id='{walletId}' не найден");

            await _walletsRepository.DepositeAsync(walletId, value);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<bool>> ChargeByTariffAsync(Guid walletId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ChargeByTariffAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var wallet = await _walletsRepository.GetWalletAsync(walletId);
            if (wallet == null)
                return CommandResult<bool>.Fail(ErrorCode.WalletNotFound, $"Кошелёк с id='{walletId}' не найден");

            var result = await _walletsRepository.ChargeByTariffAsync(walletId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<bool>(result);
        }

        public async Task<CommandResult<CreateWalletDepositResponse>> CreateWalletDepositAsync(
            CreateWalletDepositRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(CreateWalletDepositAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
            {
                return CommandResult<CreateWalletDepositResponse>.Fail(
                    ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");
            }

            if (request == null || request.WalletId == Guid.Empty)
            {
                return CommandResult<CreateWalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue, "Не указан кошелёк");
            }

            if (request.Amount <= 0)
            {
                return CommandResult<CreateWalletDepositResponse>.Fail(
                    ErrorCode.PaymentValueMustBeOverZero, "Сумма пополнения должна быть больше нуля");
            }

            if (request.Amount > 1_000_000m)
            {
                return CommandResult<CreateWalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue, "Слишком большая сумма пополнения");
            }

            var access = await AssertCanManageWalletAsync(request.WalletId);
            if (!access.Success)
            {
                return CommandResult<CreateWalletDepositResponse>.Fail(
                    access.ErrorCode, access.Message);
            }

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await _walletsRepository.GetWalletDepositByIdempotencyAsync(
                    request.WalletId, request.IdempotencyKey.Trim());
                if (existing != null)
                {
                    var response = new CreateWalletDepositResponse
                    {
                        Deposit = _mapper.Map<WalletDepositResponse>(existing),
                        ProviderPaymentId = existing.ProviderPaymentId,
                        PaidImmediately = existing.Status == WalletDepositStatus.Succeeded,
                        ConfirmationUrl = null
                    };
                    logger.Debug(correlationId, null, methodName, "Method finished (idempotent)", null, execTime.Elapsed);
                    return new CommandResult<CreateWalletDepositResponse>(response);
                }
            }

            var currency = string.IsNullOrWhiteSpace(request.Currency) ? "RUB" : request.Currency.Trim().ToUpperInvariant();
            var deposit = new WalletDeposit
            {
                WalletId = request.WalletId,
                Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
                Currency = currency,
                Status = WalletDepositStatus.Pending,
                Provider = _paymentProvider.Kind,
                IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                    ? null
                    : request.IdempotencyKey.Trim(),
                CreateDate = DateTimeOffset.UtcNow
            };
            deposit.Id = await _walletsRepository.CreateWalletDepositAsync(deposit);

            var payment = await _paymentProvider.CreatePaymentAsync(new PaymentCreationRequest
            {
                OrderId = Guid.Empty,
                WalletDepositId = deposit.Id,
                Amount = deposit.Amount,
                Currency = deposit.Currency,
                Description = "Пополнение тарифа EList",
                ReturnUrl = request.ReturnUrl,
                IdempotencyKey = deposit.IdempotencyKey,
                BuyerAccountId = _accountDataHolder.AccountId.Value,
                EventId = Guid.Empty
            });

            await _walletsRepository.UpdateWalletDepositAsync(
                deposit.Id,
                WalletDepositStatus.Pending,
                payment.ProviderPaymentId,
                paidAt: null);

            deposit.ProviderPaymentId = payment.ProviderPaymentId;

            var paidImmediately = payment.Status == PaymentProviderStatus.Succeeded;
            if (paidImmediately)
                await FulfillWalletDepositAsync(deposit);

            var done = await _walletsRepository.GetWalletDepositAsync(deposit.Id) ?? deposit;
            var result = new CreateWalletDepositResponse
            {
                Deposit = _mapper.Map<WalletDepositResponse>(done),
                ConfirmationUrl = payment.ConfirmationUrl,
                ProviderPaymentId = payment.ProviderPaymentId,
                PaidImmediately = paidImmediately || done.Status == WalletDepositStatus.Succeeded
            };

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<CreateWalletDepositResponse>(result);
        }

        public async Task<CommandResult<WalletDepositResponse>> CompleteWalletDepositAsync(
            CompleteWalletDepositRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(CompleteWalletDepositAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
            {
                return CommandResult<WalletDepositResponse>.Fail(
                    ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");
            }

            if (request == null
                || (request.DepositId == null && string.IsNullOrWhiteSpace(request.ProviderPaymentId)))
            {
                return CommandResult<WalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue, "Укажите depositId или providerPaymentId");
            }

            WalletDeposit? deposit = null;
            if (request.DepositId != null)
                deposit = await _walletsRepository.GetWalletDepositAsync(request.DepositId.Value);
            if (deposit == null && !string.IsNullOrWhiteSpace(request.ProviderPaymentId))
            {
                deposit = await _walletsRepository.GetWalletDepositByProviderPaymentAsync(
                    _paymentProvider.Kind, request.ProviderPaymentId.Trim());
            }

            if (deposit == null)
            {
                return CommandResult<WalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue, "Пополнение не найдено");
            }

            var access = await AssertCanManageWalletAsync(deposit.WalletId);
            if (!access.Success)
                return CommandResult<WalletDepositResponse>.Fail(access.ErrorCode, access.Message);

            if (deposit.Status == WalletDepositStatus.Succeeded)
            {
                logger.Debug(correlationId, null, methodName, "Method finished (already)", null, execTime.Elapsed);
                return new CommandResult<WalletDepositResponse>(_mapper.Map<WalletDepositResponse>(deposit));
            }

            if (deposit.Status != WalletDepositStatus.Pending)
            {
                return CommandResult<WalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue, $"Пополнение в статусе {deposit.Status} нельзя подтвердить");
            }

            if (!_paymentProvider.SupportsManualComplete)
            {
                return CommandResult<WalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue,
                    "Ручное подтверждение недоступно; дождитесь webhook платёжного провайдера");
            }

            var providerPaymentId = !string.IsNullOrWhiteSpace(request.ProviderPaymentId)
                ? request.ProviderPaymentId.Trim()
                : deposit.ProviderPaymentId;

            if (string.IsNullOrWhiteSpace(providerPaymentId))
            {
                return CommandResult<WalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue, "У пополнения нет идентификатора платежа");
            }

            await _paymentProvider.CompleteManuallyAsync(providerPaymentId);
            deposit.ProviderPaymentId = providerPaymentId;
            await FulfillWalletDepositAsync(deposit);

            var done = await _walletsRepository.GetWalletDepositAsync(deposit.Id) ?? deposit;
            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<WalletDepositResponse>(_mapper.Map<WalletDepositResponse>(done));
        }

        public async Task<CommandResult<List<WalletDepositResponse>>> GetWalletDepositsAsync(Guid walletId)
        {
            if (_accountDataHolder.AccountId == null)
            {
                return CommandResult<List<WalletDepositResponse>>.Fail(
                    ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");
            }

            var access = await AssertCanManageWalletAsync(walletId);
            if (!access.Success)
                return CommandResult<List<WalletDepositResponse>>.Fail(access.ErrorCode, access.Message);

            var list = await _walletsRepository.GetWalletDepositsAsync(walletId) ?? new List<WalletDeposit>();
            return new CommandResult<List<WalletDepositResponse>>(
                list.Select(d => _mapper.Map<WalletDepositResponse>(d)).ToList());
        }

        private async Task FulfillWalletDepositAsync(WalletDeposit deposit)
        {
            if (deposit.Status == WalletDepositStatus.Succeeded)
                return;

            await _walletsRepository.UpdateWalletDepositAsync(
                deposit.Id,
                WalletDepositStatus.Succeeded,
                deposit.ProviderPaymentId,
                DateTimeOffset.UtcNow);

            var credit = (double)deposit.Amount;
            await _walletsRepository.DepositeAsync(deposit.WalletId, credit);
            deposit.Status = WalletDepositStatus.Succeeded;

            // Если период истёк / не был активен и денег хватило — списание сразу, период от now.
            await _walletsRepository.ChargeByTariffAsync(deposit.WalletId);
        }

        public async Task<CommandResult<List<WalletTariffChargeResponse>>> GetWalletTariffChargesAsync(Guid walletId)
        {
            if (_accountDataHolder.AccountId == null)
            {
                return CommandResult<List<WalletTariffChargeResponse>>.Fail(
                    ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");
            }

            var access = await AssertCanManageWalletAsync(walletId);
            if (!access.Success)
                return CommandResult<List<WalletTariffChargeResponse>>.Fail(access.ErrorCode, access.Message);

            var list = await _walletsRepository.GetWalletTariffChargesAsync(walletId)
                ?? new List<WalletTariffCharge>();
            return new CommandResult<List<WalletTariffChargeResponse>>(
                list.Select(c => _mapper.Map<WalletTariffChargeResponse>(c)).ToList());
        }

        private async Task<CommandResult> AssertCanManageWalletAsync(Guid walletId)
        {
            var wallet = await _walletsRepository.GetWalletAsync(walletId);
            if (wallet == null)
                return CommandResult.Fail(ErrorCode.WalletNotFound, $"Кошелёк с id='{walletId}' не найден");

            var actorId = _accountDataHolder.AccountId!.Value;
            if (_accountDataHolder.IsPlatformModeratorOrAbove)
                return CommandResult.OK;

            var accountId = await _walletsRepository.FindAccountIdByWalletAsync(walletId);
            if (accountId != null && accountId.Value == actorId)
                return CommandResult.OK;

            var organizationId = await _walletsRepository.FindOrganizationIdByWalletAsync(walletId);
            if (organizationId != null
                && await _organizationsRepository.IsOwnerAsync(organizationId.Value, actorId))
                return CommandResult.OK;

            return CommandResult.Fail(ErrorCode.AccessError, "Нет доступа к кошельку");
        }
    }
}
