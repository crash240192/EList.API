using AutoMapper;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.Wallets;
using EList.Repositories.Interfaces;
using EList.Services.Impl.Payments;
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

            if (item.Cost < 0)
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue, "Стоимость тарифа не может быть отрицательной");

            var uniqueFree = await AssertUniqueFreeTariffAsync(item.ForOrganization, item.Cost, excludeTariffId: null);
            if (!uniqueFree.Success)
                return CommandResult<Guid?>.Fail(uniqueFree.ErrorCode, uniqueFree.Message);

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
                return CommandResult.Fail(ErrorCode.TariffValidatorNotFound, $"Валидатор тарифа с id='{item.ValidatorId}' не найден");

            if (item.Cost < 0)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Стоимость тарифа не может быть отрицательной");

            var uniqueFree = await AssertUniqueFreeTariffAsync(item.ForOrganization, item.Cost, excludeTariffId: item.Id);
            if (!uniqueFree.Success)
                return uniqueFree;

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

            var walletId = await _walletsRepository.CreateWalletAsync(forOrganization: true);
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

            if (BillingSettings.IsPeriodPurchase)
            {
                // Платные — только через CreateTariffPurchase. Free — если нет активного платного периода.
                if (tariff.Cost > 0)
                {
                    return CommandResult.Fail(
                        ErrorCode.InvalidValue,
                        "В режиме PeriodPurchase платный тариф покупается через POST /api/Wallets/tariffPurchases");
                }

                var now = DateTimeOffset.UtcNow;
                if (wallet.TariffId != null)
                {
                    var current = await _walletsRepository.GetTariffAsync(wallet.TariffId.Value);
                    if (current != null
                        && current.Cost > 0
                        && wallet.NextChargeAt != null
                        && wallet.NextChargeAt > now)
                    {
                        return CommandResult.Fail(
                            ErrorCode.InvalidValue,
                            $"Платный тариф «{current.Name}» активен до {wallet.NextChargeAt:dd.MM.yyyy HH:mm} (UTC). " +
                            "Понижение доступно после окончания оплаченного периода.");
                    }
                }

                await _walletsRepository.SetWalletTariffAsync(walletId, tariffId);
                await _walletsRepository.ClearNextChargeAtAsync(walletId);
                logger.Debug(correlationId, null, methodName, $"Method finished (free switch)", null, execTime.Elapsed);
                return CommandResult.OK;
            }

            await _walletsRepository.SetWalletTariffAsync(walletId, tariffId);
            // Legacy WalletAutoRenew: текущий период не переносим — пробуем списать новый сразу.
            await _walletsRepository.ClearNextChargeAtAsync(walletId);
            var charged = await _walletsRepository.ChargeByTariffAsync(walletId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            if (!charged && tariff.Cost > 0)
            {
                var pending = CommandResult.OK;
                pending.Message =
                    "Тариф выбран, но период не активен: недостаточно средств. " +
                    "Действует бесплатный тариф по умолчанию. Пополните баланс — выбранный тариф активируется автоматически.";
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

            var result = await LoadWalletWithBillingAsync(walletId);
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

            var wallet = await _walletsRepository.GetAccountWalletAsync(accountId);
            if (wallet == null)
                return CommandResult<Wallet?>.Fail(ErrorCode.WalletNotFound, $"Кошелёк для аккаунта с id='{accountId}' не найден");

            if (accountId != _accountDataHolder.AccountId)
                return CommandResult<Wallet?>.Fail(ErrorCode.AccessError, $"Это чужой кошелёк, нечего сюда смотреть");

            var result = await LoadWalletWithBillingAsync(wallet.Id);

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

            var wallet = await _walletsRepository.GetOrganizationWalletAsync(organizationId);
            if (wallet == null)
                return CommandResult<Wallet?>.Fail(ErrorCode.WalletNotFound, $"Кошелёк для организации с id='{organizationId}' не найден");

            var result = await LoadWalletWithBillingAsync(wallet.Id);

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

            if (BillingSettings.IsPeriodPurchase)
            {
                return CommandResult<CreateWalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue,
                    "Пополнение баланса отключено. Купите тариф на период: POST /api/Wallets/tariffPurchases");
            }

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

            var buyerAccountId = _accountDataHolder.AccountId!.Value;
            var buyerEmail = await ResolveBuyerEmailAsync(buyerAccountId);

            var payment = await _paymentProvider.CreatePaymentAsync(new PaymentCreationRequest
            {
                OrderId = Guid.Empty,
                WalletDepositId = deposit.Id,
                Amount = deposit.Amount,
                Currency = deposit.Currency,
                Description = "Пополнение тарифа EList",
                ReturnUrl = request.ReturnUrl,
                IdempotencyKey = deposit.IdempotencyKey,
                BuyerAccountId = buyerAccountId,
                EventId = Guid.Empty,
                BuyerEmail = buyerEmail
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

        public async Task<CommandResult<Guid?>> ApplyProviderWalletDepositStatusAsync(
            PaymentProvider provider,
            string providerPaymentId,
            string? orderIdFromProvider,
            string status)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ApplyProviderWalletDepositStatusAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (string.IsNullOrWhiteSpace(providerPaymentId))
            {
                return CommandResult<Guid?>.Fail(
                    ErrorCode.InvalidValue, "Нет providerPaymentId");
            }

            var paymentId = providerPaymentId.Trim();
            var normalizedStatus = string.IsNullOrWhiteSpace(status)
                ? "UNKNOWN"
                : status.Trim().ToUpperInvariant();

            WalletDeposit? deposit = null;

            if (!string.IsNullOrWhiteSpace(orderIdFromProvider)
                && orderIdFromProvider.Trim().StartsWith("wallet:", StringComparison.OrdinalIgnoreCase))
            {
                var rawId = orderIdFromProvider.Trim().Substring("wallet:".Length).Trim();
                if (Guid.TryParse(rawId, out var depositId))
                    deposit = await _walletsRepository.GetWalletDepositAsync(depositId);
            }

            if (deposit == null)
            {
                deposit = await _walletsRepository.GetWalletDepositByProviderPaymentAsync(
                    provider, paymentId);
            }

            if (deposit == null)
            {
                logger.Debug(correlationId, null, methodName,
                    "Method finished (not a wallet deposit)", null, execTime.Elapsed);
                return new CommandResult<Guid?>(null);
            }

            if (string.IsNullOrWhiteSpace(deposit.ProviderPaymentId))
            {
                deposit.ProviderPaymentId = paymentId;
                await _walletsRepository.UpdateWalletDepositAsync(
                    deposit.Id,
                    deposit.Status,
                    paymentId,
                    paidAt: null);
            }

            if (normalizedStatus == "CONFIRMED")
            {
                await FulfillWalletDepositAsync(deposit);
            }
            else if (normalizedStatus is "REJECTED" or "AUTH_FAIL")
            {
                if (deposit.Status == WalletDepositStatus.Pending)
                {
                    await _walletsRepository.UpdateWalletDepositAsync(
                        deposit.Id,
                        WalletDepositStatus.Failed,
                        deposit.ProviderPaymentId ?? paymentId,
                        paidAt: null);
                }
            }
            else if (normalizedStatus is "CANCELED" or "CANCELLED" or "DEADLINE_EXPIRED")
            {
                if (deposit.Status == WalletDepositStatus.Pending)
                {
                    await _walletsRepository.UpdateWalletDepositAsync(
                        deposit.Id,
                        WalletDepositStatus.Canceled,
                        deposit.ProviderPaymentId ?? paymentId,
                        paidAt: null);
                }
            }

            logger.Debug(correlationId, null, methodName,
                $"Method finished deposit={deposit.Id} status={normalizedStatus}", null, execTime.Elapsed);
            return new CommandResult<Guid?>(deposit.Id);
        }

        public async Task<CommandResult<WalletDepositResponse>> GetWalletDepositAsync(Guid depositId)
        {
            if (_accountDataHolder.AccountId == null)
            {
                return CommandResult<WalletDepositResponse>.Fail(
                    ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");
            }

            if (depositId == Guid.Empty)
            {
                return CommandResult<WalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue, "Не указан depositId");
            }

            var deposit = await _walletsRepository.GetWalletDepositAsync(depositId);
            if (deposit == null)
            {
                return CommandResult<WalletDepositResponse>.Fail(
                    ErrorCode.InvalidValue, "Пополнение не найдено");
            }

            var access = await AssertCanManageWalletAsync(deposit.WalletId);
            if (!access.Success)
                return CommandResult<WalletDepositResponse>.Fail(access.ErrorCode, access.Message);

            // P4: webhook мог не дойти — GetState fallback при Pending.
            await TrySyncWalletDepositFromProviderAsync(deposit);
            if (deposit.Status == WalletDepositStatus.Pending)
            {
                var refreshed = await _walletsRepository.GetWalletDepositAsync(depositId);
                if (refreshed != null)
                    deposit = refreshed;
            }

            return new CommandResult<WalletDepositResponse>(_mapper.Map<WalletDepositResponse>(deposit));
        }

        /// <summary>
        /// GetState fallback для пополнения: CONFIRMED → FulfillWalletDepositAsync.
        /// </summary>
        private async Task TrySyncWalletDepositFromProviderAsync(WalletDeposit deposit)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(TrySyncWalletDepositFromProviderAsync)}";

            if (_paymentProvider.SupportsManualComplete)
                return;
            if (deposit.Status != WalletDepositStatus.Pending)
                return;
            if (string.IsNullOrWhiteSpace(deposit.ProviderPaymentId))
                return;
            if (deposit.Provider != null && deposit.Provider != _paymentProvider.Kind)
                return;
            if (!Payments.PaymentProviderSyncGate.TryEnter(deposit.ProviderPaymentId))
                return;

            PaymentStatusInfo statusInfo;
            try
            {
                statusInfo = await _paymentProvider.GetStatusAsync(deposit.ProviderPaymentId.Trim());
            }
            catch (Exception ex)
            {
                logger.Debug(correlationId, null, methodName,
                    $"GetState sync skipped: {ex.Message}", null);
                return;
            }

            logger.Debug(correlationId, null, methodName,
                $"GetState sync payment={deposit.ProviderPaymentId} status={statusInfo.Status}", null);

            if (statusInfo.Status == PaymentProviderStatus.Succeeded)
            {
                await FulfillWalletDepositAsync(deposit);
            }
            else if (statusInfo.Status == PaymentProviderStatus.Failed)
            {
                await _walletsRepository.UpdateWalletDepositAsync(
                    deposit.Id,
                    WalletDepositStatus.Failed,
                    deposit.ProviderPaymentId,
                    paidAt: null);
                deposit.Status = WalletDepositStatus.Failed;
            }
            else if (statusInfo.Status == PaymentProviderStatus.Canceled)
            {
                await _walletsRepository.UpdateWalletDepositAsync(
                    deposit.Id,
                    WalletDepositStatus.Canceled,
                    deposit.ProviderPaymentId,
                    paidAt: null);
                deposit.Status = WalletDepositStatus.Canceled;
            }
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

            if (deposit.PurchaseKind != WalletTariffPurchaseKind.TopUp
                && deposit.TariffId != null
                && deposit.PeriodStart != null
                && deposit.PeriodEnd != null)
            {
                await FulfillTariffPeriodPurchaseAsync(deposit);
                return;
            }

            var credit = (double)deposit.Amount;
            await _walletsRepository.DepositeAsync(deposit.WalletId, credit);

            var walletAfter = await _walletsRepository.GetWalletAsync(deposit.WalletId);
            var balanceAfter = walletAfter?.Balance;

            await _walletsRepository.UpdateWalletDepositAsync(
                deposit.Id,
                WalletDepositStatus.Succeeded,
                deposit.ProviderPaymentId,
                DateTimeOffset.UtcNow,
                balanceAfter);

            deposit.Status = WalletDepositStatus.Succeeded;
            deposit.BalanceAfter = balanceAfter;
            deposit.PaidAt = DateTimeOffset.UtcNow;

            // Legacy: если период истёк и денег хватило — списание сразу.
            await _walletsRepository.ChargeByTariffAsync(deposit.WalletId);
        }

        private async Task FulfillTariffPeriodPurchaseAsync(WalletDeposit deposit)
        {
            var wallet = await _walletsRepository.GetWalletAsync(deposit.WalletId);
            if (wallet == null || deposit.TariffId == null
                || deposit.PeriodStart == null || deposit.PeriodEnd == null)
                return;

            // Credit уже заложен в Amount (к оплате). Списываем с баланса ту часть credit,
            // которая бралась из legacy Balance (остаток периода в баланс не кладём).
            var balanceCredit = Math.Min(wallet.Balance, (double)deposit.CreditApplied);
            var newBalance = Math.Max(0, wallet.Balance - balanceCredit);

            await _walletsRepository.ApplyPeriodEntitlementAsync(
                deposit.WalletId,
                deposit.TariffId.Value,
                deposit.PeriodStart.Value,
                deposit.PeriodEnd.Value,
                newBalance);

            await _walletsRepository.UpdateWalletDepositAsync(
                deposit.Id,
                WalletDepositStatus.Succeeded,
                deposit.ProviderPaymentId,
                DateTimeOffset.UtcNow,
                newBalance);

            deposit.Status = WalletDepositStatus.Succeeded;
            deposit.BalanceAfter = newBalance;
            deposit.PaidAt = DateTimeOffset.UtcNow;
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

        /// <summary>
        /// Назначить free default при отсутствии тарифа; в legacy — due charge; обогатить статус.
        /// PeriodPurchase: истёкший платный → переключить на free (без лимбо).
        /// </summary>
        private async Task<Wallet?> LoadWalletWithBillingAsync(Guid walletId)
        {
            var wallet = await _walletsRepository.GetWalletAsync(walletId);
            if (wallet == null)
                return null;

            var orgId = await _walletsRepository.FindOrganizationIdByWalletAsync(walletId);
            var forOrganization = orgId != null;
            var free = await _walletsRepository.GetDefaultFreeTariffAsync(forOrganization);
            var now = DateTimeOffset.UtcNow;

            if (BillingSettings.IsPeriodPurchase)
            {
                if (wallet.TariffId == null && free != null)
                {
                    await _walletsRepository.SetWalletTariffAsync(walletId, free.Id);
                    await _walletsRepository.ClearNextChargeAtAsync(walletId);
                }
                else if (wallet.TariffId != null)
                {
                    var current = await _walletsRepository.GetTariffAsync(wallet.TariffId.Value);
                    var expiredPaid = current != null
                        && current.Cost > 0
                        && (wallet.NextChargeAt == null || wallet.NextChargeAt <= now);
                    if (expiredPaid && free != null)
                    {
                        // Нет лимбо: entitlement сгорел → free.
                        await _walletsRepository.SetWalletTariffAsync(walletId, free.Id);
                        await _walletsRepository.ClearNextChargeAtAsync(walletId);
                    }
                }
            }
            else
            {
                if (wallet.TariffId == null)
                {
                    if (free != null)
                    {
                        await _walletsRepository.SetWalletTariffAsync(walletId, free.Id);
                        await _walletsRepository.ChargeByTariffAsync(walletId);
                    }
                }
                else
                {
                    await _walletsRepository.ChargeByTariffAsync(walletId);
                }
            }

            wallet = await _walletsRepository.GetWalletAsync(walletId);
            if (wallet == null)
                return null;

            await EnrichWalletBillingAsync(wallet, forOrganization);
            return wallet;
        }

        private async Task EnrichWalletBillingAsync(Wallet wallet, bool forOrganization)
        {
            wallet.BillingMode = BillingSettings.Mode;

            Tariff? selected = null;
            if (wallet.TariffId != null)
                selected = await _walletsRepository.GetTariffAsync(wallet.TariffId.Value);

            var free = await _walletsRepository.GetDefaultFreeTariffAsync(forOrganization);
            var now = DateTimeOffset.UtcNow;
            var selectedActive = selected != null && (
                selected.Cost <= 0
                || (wallet.NextChargeAt != null && wallet.NextChargeAt > now));

            wallet.IsSelectedTariffActive = selectedActive;
            wallet.EffectiveTariffId = selectedActive
                ? selected!.Id
                : free?.Id;

            if (selected == null)
            {
                wallet.TariffBillingStatus = free != null
                    ? $"Действует бесплатный тариф «{free.Name}»"
                    : "Тариф не назначен";
                return;
            }

            if (selected.Cost <= 0)
            {
                wallet.TariffBillingStatus = $"Бесплатный тариф «{selected.Name}» активен";
                return;
            }

            if (selectedActive)
            {
                wallet.TariffBillingStatus = BillingSettings.IsPeriodPurchase
                    ? $"Тариф «{selected.Name}» активен до {wallet.NextChargeAt:dd.MM.yyyy HH:mm} (UTC)"
                    : $"Тариф «{selected.Name}» активен до {wallet.NextChargeAt:dd.MM.yyyy HH:mm} (UTC) · следующее списание";
                return;
            }

            if (BillingSettings.IsPeriodPurchase)
            {
                wallet.TariffBillingStatus = free != null
                    ? $"Период истёк. Действует бесплатный тариф «{free.Name}»"
                    : "Период тарифа истёк";
                return;
            }

            var need = selected.Cost;
            var shortfall = Math.Max(0, need - wallet.Balance);
            wallet.TariffBillingStatus =
                $"Выбран тариф «{selected.Name}», но период не активен: недостаточно средств" +
                (shortfall > 0 ? $" (нужно ещё {shortfall:0.##} ₽)" : "") +
                ". Действует бесплатный тариф по умолчанию. После пополнения выбранный тариф активируется автоматически.";
        }

        public async Task<CommandResult<TariffPurchaseQuoteResponse>> QuoteTariffPurchaseAsync(
            TariffPurchaseQuoteRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(QuoteTariffPurchaseAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (!BillingSettings.IsPeriodPurchase)
            {
                return CommandResult<TariffPurchaseQuoteResponse>.Fail(
                    ErrorCode.InvalidValue,
                    "Покупка периода доступна только при billing.mode=PeriodPurchase");
            }

            if (_accountDataHolder.AccountId == null)
            {
                return CommandResult<TariffPurchaseQuoteResponse>.Fail(
                    ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");
            }

            if (request == null || request.WalletId == Guid.Empty || request.TariffId == Guid.Empty)
            {
                return CommandResult<TariffPurchaseQuoteResponse>.Fail(
                    ErrorCode.InvalidValue, "Укажите walletId и tariffId");
            }

            var access = await AssertCanManageWalletAsync(request.WalletId);
            if (!access.Success)
                return CommandResult<TariffPurchaseQuoteResponse>.Fail(access.ErrorCode, access.Message);

            var built = await BuildTariffPurchaseQuoteAsync(request.WalletId, request.TariffId);
            if (!built.Success)
                return CommandResult<TariffPurchaseQuoteResponse>.Fail(built.ErrorCode, built.Message);

            return new CommandResult<TariffPurchaseQuoteResponse>(built.Result!);
        }

        public async Task<CommandResult<CreateTariffPurchaseResponse>> CreateTariffPurchaseAsync(
            TariffPurchaseRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(CreateTariffPurchaseAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (!BillingSettings.IsPeriodPurchase)
            {
                return CommandResult<CreateTariffPurchaseResponse>.Fail(
                    ErrorCode.InvalidValue,
                    "Покупка периода доступна только при billing.mode=PeriodPurchase");
            }

            if (_accountDataHolder.AccountId == null)
            {
                return CommandResult<CreateTariffPurchaseResponse>.Fail(
                    ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");
            }

            if (request == null || request.WalletId == Guid.Empty || request.TariffId == Guid.Empty)
            {
                return CommandResult<CreateTariffPurchaseResponse>.Fail(
                    ErrorCode.InvalidValue, "Укажите walletId и tariffId");
            }

            var access = await AssertCanManageWalletAsync(request.WalletId);
            if (!access.Success)
                return CommandResult<CreateTariffPurchaseResponse>.Fail(access.ErrorCode, access.Message);

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await _walletsRepository.GetWalletDepositByIdempotencyAsync(
                    request.WalletId, request.IdempotencyKey.Trim());
                if (existing != null)
                {
                    Wallet? w = null;
                    if (existing.Status == WalletDepositStatus.Succeeded)
                        w = await LoadWalletWithBillingAsync(request.WalletId);
                    return new CommandResult<CreateTariffPurchaseResponse>(new CreateTariffPurchaseResponse
                    {
                        Deposit = _mapper.Map<WalletDepositResponse>(existing),
                        PaidImmediately = existing.Status == WalletDepositStatus.Succeeded,
                        Wallet = w
                    });
                }
            }

            var quoteResult = await BuildTariffPurchaseQuoteAsync(request.WalletId, request.TariffId);
            if (!quoteResult.Success || quoteResult.Result == null)
            {
                return CommandResult<CreateTariffPurchaseResponse>.Fail(
                    quoteResult.ErrorCode, quoteResult.Message);
            }

            var quote = quoteResult.Result;
            if (string.Equals(quote.Kind, "Free", StringComparison.OrdinalIgnoreCase))
            {
                var set = await SetWalletTariffAsync(request.WalletId, request.TariffId);
                if (!set.Success)
                    return CommandResult<CreateTariffPurchaseResponse>.Fail(set.ErrorCode, set.Message);

                var wallet = await LoadWalletWithBillingAsync(request.WalletId);
                return new CommandResult<CreateTariffPurchaseResponse>(new CreateTariffPurchaseResponse
                {
                    Deposit = new WalletDepositResponse
                    {
                        Id = Guid.Empty,
                        WalletId = request.WalletId,
                        Amount = 0,
                        Currency = "RUB",
                        Status = WalletDepositStatus.Succeeded,
                        PurchaseKind = WalletTariffPurchaseKind.Purchase,
                        CreateDate = DateTimeOffset.UtcNow,
                        PaidAt = DateTimeOffset.UtcNow,
                        TariffId = request.TariffId
                    },
                    PaidImmediately = true,
                    Wallet = wallet
                });
            }

            var kind = quote.Kind switch
            {
                "Extend" => WalletTariffPurchaseKind.Extend,
                "Upgrade" => WalletTariffPurchaseKind.Upgrade,
                _ => WalletTariffPurchaseKind.Purchase
            };

            // Extend: сохраняем LastChargeDate; Purchase/Upgrade: период с now.
            var walletNow = await _walletsRepository.GetWalletAsync(request.WalletId);
            var entitlementStart = kind == WalletTariffPurchaseKind.Extend
                && walletNow?.LastChargeDate != null
                    ? walletNow.LastChargeDate.Value
                    : DateTimeOffset.UtcNow;

            var deposit = new WalletDeposit
            {
                WalletId = request.WalletId,
                Amount = Math.Round(quote.AmountDue, 2, MidpointRounding.AwayFromZero),
                Currency = "RUB",
                Status = WalletDepositStatus.Pending,
                Provider = _paymentProvider.Kind,
                IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                    ? null
                    : request.IdempotencyKey.Trim(),
                CreateDate = DateTimeOffset.UtcNow,
                TariffId = request.TariffId,
                PurchaseKind = kind,
                CreditApplied = Math.Round(quote.Credit, 2, MidpointRounding.AwayFromZero),
                PeriodStart = entitlementStart,
                PeriodEnd = quote.NewActiveUntil
            };
            deposit.Id = await _walletsRepository.CreateWalletDepositAsync(deposit);

            if (deposit.Amount <= 0)
            {
                await FulfillTariffPeriodPurchaseAsync(deposit);
                var doneZero = await _walletsRepository.GetWalletDepositAsync(deposit.Id) ?? deposit;
                var walletZero = await LoadWalletWithBillingAsync(request.WalletId);
                logger.Debug(correlationId, null, methodName, "Method finished (credit covers)", null, execTime.Elapsed);
                return new CommandResult<CreateTariffPurchaseResponse>(new CreateTariffPurchaseResponse
                {
                    Deposit = _mapper.Map<WalletDepositResponse>(doneZero),
                    PaidImmediately = true,
                    Wallet = walletZero
                });
            }

            var buyerAccountId = _accountDataHolder.AccountId!.Value;
            var buyerEmail = await ResolveBuyerEmailAsync(buyerAccountId);
            var payment = await _paymentProvider.CreatePaymentAsync(new PaymentCreationRequest
            {
                OrderId = Guid.Empty,
                WalletDepositId = deposit.Id,
                Amount = deposit.Amount,
                Currency = deposit.Currency,
                Description = $"Тариф «{quote.TariffName}» на {quote.PeriodDays} дн.",
                ReturnUrl = request.ReturnUrl,
                IdempotencyKey = deposit.IdempotencyKey,
                BuyerAccountId = buyerAccountId,
                EventId = Guid.Empty,
                BuyerEmail = buyerEmail
            });

            await _walletsRepository.UpdateWalletDepositAsync(
                deposit.Id,
                WalletDepositStatus.Pending,
                payment.ProviderPaymentId,
                paidAt: null);
            deposit.ProviderPaymentId = payment.ProviderPaymentId;

            var paidImmediately = payment.Status == PaymentProviderStatus.Succeeded;
            if (paidImmediately)
                await FulfillTariffPeriodPurchaseAsync(deposit);

            var done = await _walletsRepository.GetWalletDepositAsync(deposit.Id) ?? deposit;
            Wallet? walletAfter = null;
            if (done.Status == WalletDepositStatus.Succeeded)
                walletAfter = await LoadWalletWithBillingAsync(request.WalletId);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<CreateTariffPurchaseResponse>(new CreateTariffPurchaseResponse
            {
                Deposit = _mapper.Map<WalletDepositResponse>(done),
                ConfirmationUrl = payment.ConfirmationUrl,
                ProviderPaymentId = payment.ProviderPaymentId,
                PaidImmediately = paidImmediately || done.Status == WalletDepositStatus.Succeeded,
                Wallet = walletAfter
            });
        }

        private async Task<CommandResult<TariffPurchaseQuoteResponse>> BuildTariffPurchaseQuoteAsync(
            Guid walletId,
            Guid tariffId)
        {
            var wallet = await _walletsRepository.GetWalletAsync(walletId);
            if (wallet == null)
            {
                return CommandResult<TariffPurchaseQuoteResponse>.Fail(
                    ErrorCode.WalletNotFound, $"Кошелёк с id='{walletId}' не найден");
            }

            var target = await _walletsRepository.GetTariffAsync(tariffId);
            if (target == null)
            {
                return CommandResult<TariffPurchaseQuoteResponse>.Fail(
                    ErrorCode.TariffNotFound, $"Тариф с id='{tariffId}' не найден");
            }

            var orgId = await _walletsRepository.FindOrganizationIdByWalletAsync(walletId);
            if (target.ForOrganization && orgId == null)
            {
                return CommandResult<TariffPurchaseQuoteResponse>.Fail(
                    ErrorCode.InvalidValue, "Этот тариф предназначен для организации");
            }
            if (!target.ForOrganization && orgId != null)
            {
                return CommandResult<TariffPurchaseQuoteResponse>.Fail(
                    ErrorCode.InvalidValue, "Этот тариф предназначен для личного аккаунта");
            }

            var now = DateTimeOffset.UtcNow;
            Tariff? current = null;
            if (wallet.TariffId != null)
                current = await _walletsRepository.GetTariffAsync(wallet.TariffId.Value);

            var currentActive = current != null && (
                current.Cost <= 0
                || (wallet.NextChargeAt != null && wallet.NextChargeAt > now));

            var period = target.Period <= TimeSpan.Zero ? TimeSpan.FromDays(30) : target.Period;
            var balanceCredit = (decimal)Math.Max(0, wallet.Balance);

            if (target.Cost <= 0)
            {
                if (currentActive && current != null && current.Cost > 0)
                {
                    return CommandResult<TariffPurchaseQuoteResponse>.Fail(
                        ErrorCode.InvalidValue,
                        $"Платный тариф «{current.Name}» активен до {wallet.NextChargeAt:dd.MM.yyyy HH:mm} (UTC). " +
                        "Переход на бесплатный — после окончания периода.");
                }

                return new CommandResult<TariffPurchaseQuoteResponse>(new TariffPurchaseQuoteResponse
                {
                    WalletId = walletId,
                    TariffId = target.Id,
                    TariffName = target.Name,
                    Kind = "Free",
                    ListPrice = 0,
                    Credit = 0,
                    AmountDue = 0,
                    PeriodDays = target.PeriodDays,
                    CurrentActiveUntil = wallet.NextChargeAt,
                    NewActiveUntil = now,
                    Message = "Бесплатный тариф без оплаты"
                });
            }

            if (currentActive && current != null && current.Id == target.Id)
            {
                var baseUntil = wallet.NextChargeAt != null && wallet.NextChargeAt > now
                    ? wallet.NextChargeAt.Value
                    : now;
                var listPrice = (decimal)target.Cost;
                var credit = Math.Min(balanceCredit, listPrice);
                var amountDue = Math.Max(0, Math.Round(listPrice - credit, 2, MidpointRounding.AwayFromZero));
                return new CommandResult<TariffPurchaseQuoteResponse>(new TariffPurchaseQuoteResponse
                {
                    WalletId = walletId,
                    TariffId = target.Id,
                    TariffName = target.Name,
                    Kind = "Extend",
                    ListPrice = listPrice,
                    Credit = Math.Round(credit, 2, MidpointRounding.AwayFromZero),
                    AmountDue = amountDue,
                    PeriodDays = target.PeriodDays,
                    CurrentActiveUntil = wallet.NextChargeAt,
                    NewActiveUntil = baseUntil + period,
                    Message = credit > 0
                        ? $"Продление: учтён остаток баланса {credit:0.##} ₽"
                        : "Продление текущего тарифа"
                });
            }

            if (currentActive && current != null && current.Cost > 0)
            {
                if (target.Cost <= current.Cost)
                {
                    return CommandResult<TariffPurchaseQuoteResponse>.Fail(
                        ErrorCode.InvalidValue,
                        $"Сейчас активен «{current.Name}» до {wallet.NextChargeAt:dd.MM.yyyy HH:mm} (UTC). " +
                        "Понижение тарифа — после окончания периода; повышение — через upgrade.");
                }

                var remaining = (wallet.NextChargeAt!.Value - now).TotalDays;
                if (remaining < 0)
                    remaining = 0;
                var curPeriodDays = Math.Max(1, current.PeriodDays);
                var periodCredit = (decimal)(current.Cost * (remaining / curPeriodDays));
                var credit = periodCredit + balanceCredit;
                var listPrice = (decimal)target.Cost;
                var amountDue = Math.Max(0, Math.Round(listPrice - credit, 2, MidpointRounding.AwayFromZero));
                return new CommandResult<TariffPurchaseQuoteResponse>(new TariffPurchaseQuoteResponse
                {
                    WalletId = walletId,
                    TariffId = target.Id,
                    TariffName = target.Name,
                    Kind = "Upgrade",
                    ListPrice = listPrice,
                    Credit = Math.Round(credit, 2, MidpointRounding.AwayFromZero),
                    AmountDue = amountDue,
                    PeriodDays = target.PeriodDays,
                    CurrentActiveUntil = wallet.NextChargeAt,
                    NewActiveUntil = now + period,
                    Message =
                        $"Upgrade: скидка {credit:0.##} ₽ (остаток «{current.Name}» ≈ {remaining:0.#} дн." +
                        (balanceCredit > 0 ? $" + баланс {balanceCredit:0.##} ₽" : "") + ")"
                });
            }

            {
                var listPrice = (decimal)target.Cost;
                var credit = Math.Min(balanceCredit, listPrice);
                var amountDue = Math.Max(0, Math.Round(listPrice - credit, 2, MidpointRounding.AwayFromZero));
                return new CommandResult<TariffPurchaseQuoteResponse>(new TariffPurchaseQuoteResponse
                {
                    WalletId = walletId,
                    TariffId = target.Id,
                    TariffName = target.Name,
                    Kind = "Purchase",
                    ListPrice = listPrice,
                    Credit = Math.Round(credit, 2, MidpointRounding.AwayFromZero),
                    AmountDue = amountDue,
                    PeriodDays = target.PeriodDays,
                    CurrentActiveUntil = wallet.NextChargeAt,
                    NewActiveUntil = now + period,
                    Message = credit > 0
                        ? $"Покупка: учтён остаток баланса {credit:0.##} ₽"
                        : "Покупка тарифа на период"
                });
            }
        }

        private async Task<CommandResult> AssertUniqueFreeTariffAsync(
            bool forOrganization, double cost, Guid? excludeTariffId)
        {
            if (cost > 0)
                return CommandResult.OK;

            var other = await _walletsRepository.FindOtherZeroCostTariffAsync(forOrganization, excludeTariffId);
            if (other == null)
                return CommandResult.OK;

            var scope = forOrganization ? "организаций" : "личных аккаунтов";
            return CommandResult.Fail(
                ErrorCode.DefaultFreeTariffAlreadyExists,
                $"В контуре {scope} уже есть бесплатный тариф «{other.Name}». Допустим только один тариф со стоимостью 0.");
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

        private async Task<string?> ResolveBuyerEmailAsync(Guid accountId)
        {
            var account = await _accountsRepository.GetAccountAsync(accountId);
            var login = account?.Login?.Trim();
            if (!string.IsNullOrWhiteSpace(login) && login.Contains('@'))
                return login;
            return null;
        }
    }
}
