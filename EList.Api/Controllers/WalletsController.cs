using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Wallets;
using EList.Services.Impl;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [LoggerHandlerWebApiFilter]
    public class WalletsController : ControllerBase
    {
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IWalletsService _walletsService;

        public WalletsController(IWalletsService walletsService,
            IDataConnectionProvider connectionProvider)
        {
            _walletsService = walletsService;
            _connectionProvider = connectionProvider;
        }

        #region tariff
        /// <summary>
        /// Создание тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("tariff/create")]
        public async Task<CommandResult<Guid?>> CreateTariffAsync(Tariff request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.CreateTariffAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;

            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Обновление тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("tariff/update")]
        public async Task<CommandResult> UpdateTariffAsync(Tariff request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.UpdateTariffAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return CommandResult.OK;

            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Получение тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("tariff/{tariffId}")]
        public async Task<CommandResult<Tariff?>> GetTariffAsync(Guid tariffId)
        {
            var result = await _walletsService.GetTariffAsync(tariffId);
            return result;
        }

        /// <summary>
        /// Получение тарифа кошелька
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("tariff/byWalletId/{walletId}")]
        public async Task<CommandResult<Tariff?>> GetWalletTariffAsync(Guid walletId)
        {
            var result = await _walletsService.GetWalletTariffAsync(walletId);
            return result;

        }

        /// <summary>
        /// Список тарифов
        /// </summary>
        /// <returns></returns>
        [HttpGet("tariffs")]
        public async Task<CommandResult<List<Tariff>?>> GetTariffsAsync(bool forOrganization = false)
        {
            var result = await _walletsService.GetTariffsAsync(forOrganization);
            return result;

        }
        #endregion

        #region tariff validator
        /// <summary>
        /// Создание валидатора тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("tariffValidator/create")]
        public async Task<CommandResult<Guid?>> CreateTariffValidatorAsync(TariffValidator request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.CreateTariffValidatorAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;

            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Обновление валидатора тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("tariffValidator/update")]
        public async Task<CommandResult> UpdateTariffValidatorAsync(TariffValidator request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.UpdateTariffValidatorAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return CommandResult.OK;

            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Получение валидатора тарифа
        /// </summary>
        /// <param name="tariffValidatorId"></param>
        /// <returns></returns>
        [HttpGet("tariffValidator/{tariffValidatorId}")]
        public async Task<CommandResult<TariffValidator?>> GetTariffValidatorAsync(Guid tariffValidatorId)
        {
            var result = await _walletsService.GetTariffValidatorAsync(tariffValidatorId);
            return result;
        }

        /// <summary>
        /// Получение валидатора тарифа по tariffId
        /// </summary>
        /// <param name="tariffId"></param>
        /// <returns></returns>
        [HttpGet("tariffValidator/byTariffId/{tariffId}")]
        public async Task<CommandResult<TariffValidator?>> GetTariffValidatorByTariffIdAsync(Guid tariffId)
        {
            var result = await _walletsService.GetTariffValidatorByTariffIdAsync(tariffId);
            return result;

        }
        #endregion

        #region wallets
        /// <summary>
        /// Создание кошелька аккаунта
        /// </summary>
        /// <returns></returns>
        [HttpGet("create")]
        public async Task<CommandResult<Guid?>> CreateAccountWalletAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.CreateAccountWalletAsync();
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;

            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Создание кошелька организации
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("byOrganization/create")]
        public async Task<CommandResult<Guid?>> CreateOrganizationWalletAsync(Wallet request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.CreateOrganizationWalletAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;

            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Изменение тарифа кошелька
        /// </summary>
        /// <returns></returns>
        [HttpPut("setTariff")]
        public async Task<CommandResult> SetWalletTariffAsync(Guid walletId, Guid tariffId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.SetWalletTariffAsync(walletId, tariffId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;

            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Получение данных кошелька
        /// </summary>
        /// <returns></returns>
        [HttpGet("/{walletId}")]
        public async Task<CommandResult<Wallet?>> GetWalletAsync(Guid walletId)
        {
            var result = await _walletsService.GetWalletAsync(walletId);
            return result;
        }

        /// <summary>
        /// Получение данных кошелька
        /// </summary>
        /// <returns></returns>
        [HttpGet("byAccount/{accountId}")]
        public async Task<CommandResult<Wallet?>> GetAccountWalletAsync(Guid accountId)
        {
            var result = await _walletsService.GetAccountWalletAsync(accountId);
            return result;

        }

        /// <summary>
        /// Получение данных кошелька
        /// </summary>
        /// <returns></returns>
        [HttpGet("byOrganization/{organizationId}")]
        public async Task<CommandResult<Wallet?>> GetOrganizationWalletAsync(Guid organizationId)
        {
            var result = await _walletsService.GetOrganizationWalletAsync(organizationId);
            return result;

        }

        /// <summary>
        /// Предварительный расчёт покупки / продления / upgrade тарифа (PeriodPurchase).
        /// </summary>
        [HttpPost("tariffPurchases/quote")]
        public async Task<CommandResult<TariffPurchaseQuoteResponse>> QuoteTariffPurchaseAsync(
            [FromBody] TariffPurchaseQuoteRequest request)
        {
            var result = await _walletsService.QuoteTariffPurchaseAsync(request);
            return result;

        }

        /// <summary>
        /// Купить / продлить / повысить тариф на период (PeriodPurchase).
        /// При AmountDue=0 активирует сразу; иначе — confirmationUrl эквайринга.
        /// </summary>
        [HttpPost("tariffPurchases")]
        public async Task<CommandResult<CreateTariffPurchaseResponse>> CreateTariffPurchaseAsync(
            [FromBody] TariffPurchaseRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.CreateTariffPurchaseAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Расчёт отмены тарифа и возврата за неиспользованные дни (PeriodPurchase).
        /// </summary>
        [HttpPost("tariffPurchases/cancel/quote")]
        public async Task<CommandResult<TariffCancelQuoteResponse>> QuoteTariffCancelAsync(
            [FromBody] TariffCancelQuoteRequest request)
        {
            var result = await _walletsService.QuoteTariffCancelAsync(request);
            return result;
        }

        /// <summary>
        /// Отменить платный тариф: переход на free + пропорциональный возврат на карту.
        /// </summary>
        [HttpPost("tariffPurchases/cancel")]
        public async Task<CommandResult<CreateTariffCancelResponse>> CancelTariffPurchaseAsync(
            [FromBody] TariffCancelRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.CancelTariffPurchaseAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>История возвратов по тарифу кошелька.</summary>
        [HttpGet("{walletId}/tariffRefunds")]
        public async Task<CommandResult<List<WalletTariffRefundResponse>>> GetWalletTariffRefundsAsync(
            Guid walletId)
        {
            var result = await _walletsService.GetWalletTariffRefundsAsync(walletId);
            return result;
        }

        /// <summary>
        /// Пополнение тарифного кошелька (legacy WalletAutoRenew). В PeriodPurchase отключено.
        /// </summary>
        [HttpPost("deposits")]
        public async Task<CommandResult<CreateWalletDepositResponse>> CreateWalletDepositAsync(
            [FromBody] CreateWalletDepositRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.CreateWalletDepositAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>Stub/debug: подтвердить пополнение тарифного кошелька.</summary>
        [HttpPost("deposits/complete")]
        public async Task<CommandResult<WalletDepositResponse>> CompleteWalletDepositAsync(
            [FromBody] CompleteWalletDepositRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _walletsService.CompleteWalletDepositAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Одно пополнение по id (poll после T-Bank return с depositId в SuccessURL).
        /// </summary>
        [HttpGet("deposits/{depositId}")]
        public async Task<CommandResult<WalletDepositResponse>> GetWalletDepositAsync(Guid depositId)
        {
            var result = await _walletsService.GetWalletDepositAsync(depositId);
            return result;
        }

        /// <summary>История пополнений тарифного кошелька.</summary>
        [HttpGet("{walletId}/deposits")]
        public async Task<CommandResult<List<WalletDepositResponse>>> GetWalletDepositsAsync(Guid walletId)
        {
            var result = await _walletsService.GetWalletDepositsAsync(walletId);
            return result;

        }

        /// <summary>История списаний тарифа платформы.</summary>
        [HttpGet("{walletId}/tariffCharges")]
        public async Task<CommandResult<List<WalletTariffChargeResponse>>> GetWalletTariffChargesAsync(Guid walletId)
        {
            var result = await _walletsService.GetWalletTariffChargesAsync(walletId);
            return result;

        }
        #endregion
    }
}
