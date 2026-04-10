using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
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
        private readonly IAccountDataHolder _accountDataHolder;
        public WalletsService(ICorrelationIdProvider correlationIdProvider,
            IWalletsRepository walletsRepository,
            IAccountsRepository accountsRepository,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider;
            _walletsRepository = walletsRepository;
            _accountsRepository = accountsRepository;
            _accountDataHolder = accountDataHolder;
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
            if (tariffValidator != null)
                return CommandResult.Fail(ErrorCode.TariffValidatorNotFound, $"Валидатор тарифа с id='{item.Id}' не найден");

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


        public async Task<CommandResult<Guid?>> CreateAccountWalletAsync(Wallet item)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAccountWalletAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (item.TariffId != null)
            {
                var tariff = await _walletsRepository.GetTariffAsync(item.TariffId.Value);
                if (tariff == null)
                    return CommandResult<Guid?>.Fail(ErrorCode.TariffNotFound, $"Тариф с id='{item.TariffId}' не найден");
            }

            var account = await _accountsRepository.GetAccountAsync(_accountDataHolder.AccountId);

            if (account.WalletId != null)
            {
                var result = await _walletsRepository.CreateWalletAsync(item);
                await _accountsRepository.SetAccountWalletAsync(_accountDataHolder.AccountId, result);
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

            //if (item.TariffId != null)
            //{
            //    var tariff = await _walletsRepository.GetTariffAsync(item.TariffId.Value);
            //    if (tariff == null)
            //        return CommandResult<Guid?>.Fail(ErrorCode.TariffNotFound, $"Тариф с id='{item.TariffId}' не найден");
            //}

            //var account = await _accountsRepository.GetAccountAsync(_accountDataHolder.AccountId);

            //if (account.WalletId != null)
            //{
            //    var result = await _walletsRepository.CreateWalletAsync(item);
            //    await _accountsRepository.SetAccountWalletAsync(_accountDataHolder.AccountId, result);
            //    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            //    return new CommandResult<Guid?>(result);
            //}
            //else
            //{
            //    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            //    var result = new CommandResult<Guid?>(account.WalletId);
            //    result.Message = $"Для текущего аккаунта уже существует кошелёк";
            //    return result;
            //}

            throw new NotImplementedException();
        }

        public async Task<CommandResult> SetWalletTariffAsync(Guid walletId, Guid tariffId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetWalletTariffAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var tariff = await _walletsRepository.GetTariffAsync(tariffId);
            if (tariff == null)
                return CommandResult.Fail(ErrorCode.TariffNotFound, $"Тариф с id='{tariffId}' не найден");

            var wallet = await _walletsRepository.GetWalletAsync(walletId);
            if (wallet == null)
                return CommandResult.Fail(ErrorCode.WalletNotFound, $"Кошелёк с id='{walletId}' не найден");

            await _walletsRepository.SetWalletTariffAsync(walletId, tariffId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
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

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Wallet?>(result);
        }

        
        public async Task<CommandResult<Wallet?>> GetOrganizationWalletAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizationWalletAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            // TODO: Добавить сюда проверку наличия организации
            //var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            //if (organization == null)
            //    return CommandResult<Wallet?>.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

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
    }
}
