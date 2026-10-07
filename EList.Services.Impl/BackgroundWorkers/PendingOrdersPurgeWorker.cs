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
    /// TTL неоплаченных заказов: Pending/Authorized старше N минут → Cancel у провайдера + Canceled.
    /// Освобождает soft-hold мест при брошенной оплате.
    /// </summary>
    public class PendingOrdersPurgeWorker : PeriodicBackgroundWorkerBase
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const string LOGGER_NAME = "EList.Services.Impl.BackgroundWorkers.PendingOrdersPurgeWorker.";

        public PendingOrdersPurgeWorker(
            IServiceScopeFactory scopeFactory,
            ICorrelationIdProvider correlationIdProvider)
            : base(scopeFactory, correlationIdProvider, log, LOGGER_NAME)
        {
        }

        protected override string ConfigSectionName => "pendingOrdersPurge";
        protected override string WorkerName => "PendingOrdersPurge";

        protected override async Task ExecuteIterationAsync(IServiceProvider scopedServices, CancellationToken stoppingToken)
        {
            var methodName = $"{LOGGER_NAME}{nameof(ExecuteIterationAsync)}";
            var correlationId = scopedServices.GetRequiredService<ICorrelationIdProvider>().Get();
            var logger = new NLogLoggerWrapper(log);

            var olderThanMinutes = 30;
            var batchSize = 50;
            if (ConfigurationManager.AppSettings.Contains("pendingOrdersPurge:olderThanMinutes")
                && int.TryParse(ConfigurationManager.AppSettings["pendingOrdersPurge:olderThanMinutes"], out var m)
                && m > 0)
            {
                olderThanMinutes = m;
            }

            if (ConfigurationManager.AppSettings.Contains("pendingOrdersPurge:batchSize")
                && int.TryParse(ConfigurationManager.AppSettings["pendingOrdersPurge:batchSize"], out var b)
                && b > 0)
            {
                batchSize = b;
            }

            var ordersService = scopedServices.GetRequiredService<IOrdersService>();
            var connectionProvider = scopedServices.GetRequiredService<IDataConnectionProvider>();

            await connectionProvider.StartNewTransactionAsync();
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                var canceled = await ordersService.PurgeExpiredPendingOrdersAsync(
                    TimeSpan.FromMinutes(olderThanMinutes),
                    batchSize);
                await connectionProvider.CommitTransactionAsync();

                logger.Info(correlationId, null, methodName,
                    $"Pending orders purge done canceled={canceled} olderThanMinutes={olderThanMinutes}", null);
            }
            catch
            {
                await connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
