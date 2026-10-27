using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Repositories.Interfaces;
using EList.Services.Impl.Payments;
using EList.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace EList.Services.Impl.BackgroundWorkers
{
    /// <summary>
    /// Фоновый воркер тарифного контура:
    /// <list type="bullet">
    /// <item><see cref="BillingSettings.ModeWalletAutoRenew"/> — автосписание с баланса (legacy).</item>
    /// <item><see cref="BillingSettings.ModePeriodPurchase"/> — сброс истёкших платных entitlement → free
    /// (тот же переход, что при чтении кошелька в <c>LoadWalletWithBillingAsync</c>).</item>
    /// </list>
    /// </summary>
    public class DebtCollectorWorker : PeriodicBackgroundWorkerBase, IDebtCollectorUtility
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const string LOGGER_NAME = "EList.Services.Impl.BackgroundWorkers.DebtCollectorWorker.";

        public DebtCollectorWorker(
            IServiceScopeFactory scopeFactory,
            ICorrelationIdProvider correlationIdProvider)
            : base(scopeFactory, correlationIdProvider, log, LOGGER_NAME)
        {
        }

        protected override string ConfigSectionName => "debtCollector";
        protected override string WorkerName => "DebtCollector";

        bool IDebtCollectorUtility.Active => Active;

        void IDebtCollectorUtility.Start()
        {
            // HostedService стартует сам; метод оставлен для совместимости.
            ManualStart();
        }

        void IDebtCollectorUtility.Stop()
        {
            ManualStop();
        }

        protected override async Task ExecuteIterationAsync(IServiceProvider scopedServices, CancellationToken stoppingToken)
        {
            var methodName = $"{LOGGER_NAME}{nameof(ExecuteIterationAsync)}";
            var correlationId = scopedServices.GetRequiredService<ICorrelationIdProvider>().Get();
            var logger = new NLogLoggerWrapper(log);

            var walletsRepository = scopedServices.GetRequiredService<IWalletsRepository>();
            var walletsService = scopedServices.GetRequiredService<IWalletsService>();

            if (BillingSettings.IsPeriodPurchase)
            {
                // Не ChargeByTariff: период уже оплачен, автопродления нет.
                // Сбрасываем просроченные платные entitlement → free (как при GET wallet).
                var expired = await walletsRepository.GetExpiredPeriodPurchaseWalletsAsync();
                if (expired == null || expired.Count == 0)
                {
                    logger.Debug(correlationId, null, methodName,
                        "PeriodPurchase: no expired paid entitlements", null);
                    return;
                }

                logger.Info(correlationId, null, methodName,
                    $"PeriodPurchase: expiring {expired.Count} wallet(s)", null);

                foreach (var wallet in expired)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    try
                    {
                        // GetWalletAsync → LoadWalletWithBillingAsync: paid+due → free.
                        await walletsService.GetWalletAsync(wallet.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(correlationId, null, methodName,
                            $"Failed to expire wallet '{wallet.Id}': {ex.Message}", null, ex);
                    }

                    await DelayBetweenItemsAsync(stoppingToken);
                }

                return;
            }

            var wallets = await walletsRepository.GetOverdueWalletsAsync();
            if (wallets == null || wallets.Count == 0)
            {
                logger.Debug(correlationId, null, methodName, "No overdue wallets found", null);
                return;
            }

            logger.Info(correlationId, null, methodName, $"Processing {wallets.Count} overdue wallet(s)", null);

            foreach (var wallet in wallets)
            {
                stoppingToken.ThrowIfCancellationRequested();
                try
                {
                    await walletsService.ChargeByTariffAsync(wallet.Id);
                }
                catch (Exception ex)
                {
                    logger.Error(correlationId, null, methodName,
                        $"Failed to charge wallet '{wallet.Id}': {ex.Message}", null, ex);
                }

                await DelayBetweenItemsAsync(stoppingToken);
            }
        }
    }
}
