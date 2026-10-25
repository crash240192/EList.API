using EList.Common.Configuration;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.DbDataProvider.Interfaces;
using EList.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace EList.Services.Impl.BackgroundWorkers
{
    /// <summary>
    /// P4 Мультирасчёты: sync Initiated A2C → retry payout → closeSpDeal по TTL сделки.
    /// Не трогает неоплаченные заказы (ими занимается <see cref="PendingOrdersPurgeWorker"/>).
    /// </summary>
    public class DealMaintenanceWorker : PeriodicBackgroundWorkerBase
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const string LOGGER_NAME = "EList.Services.Impl.BackgroundWorkers.DealMaintenanceWorker.";

        public DealMaintenanceWorker(
            IServiceScopeFactory scopeFactory,
            ICorrelationIdProvider correlationIdProvider)
            : base(scopeFactory, correlationIdProvider, log, LOGGER_NAME)
        {
        }

        protected override string ConfigSectionName => "dealMaintenance";
        protected override string WorkerName => "DealMaintenance";

        protected override async Task ExecuteIterationAsync(IServiceProvider scopedServices, CancellationToken stoppingToken)
        {
            var methodName = $"{LOGGER_NAME}{nameof(ExecuteIterationAsync)}";
            var correlationId = scopedServices.GetRequiredService<ICorrelationIdProvider>().Get();
            var logger = new NLogLoggerWrapper(log);

            var batchSize = 50;
            if (ConfigurationManager.AppSettings.Contains("dealMaintenance:batchSize")
                && int.TryParse(ConfigurationManager.AppSettings["dealMaintenance:batchSize"], out var b)
                && b > 0)
            {
                batchSize = b;
            }

            var payoutService = scopedServices.GetRequiredService<IOrderDealPayoutService>();
            var connectionProvider = scopedServices.GetRequiredService<IDataConnectionProvider>();

            await connectionProvider.StartNewTransactionAsync();
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                var synced = await payoutService.SyncInitiatedPayoutsAsync(batchSize);
                stoppingToken.ThrowIfCancellationRequested();
                var retried = await payoutService.RetryOpenDealPayoutsAsync(batchSize);
                stoppingToken.ThrowIfCancellationRequested();
                var closed = await payoutService.CloseExpiredOpenDealsAsync(batchSize);
                await connectionProvider.CommitTransactionAsync();

                logger.Info(correlationId, null, methodName,
                    $"Deal maintenance done synced={synced} retried={retried} closed={closed} batchSize={batchSize}",
                    null);
            }
            catch
            {
                await connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
