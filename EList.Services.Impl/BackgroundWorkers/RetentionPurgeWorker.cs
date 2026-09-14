using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.DbDataProvider.Interfaces;
using EList.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace EList.Services.Impl.BackgroundWorkers
{
    /// <summary>
    /// Периодическая очистка: просроченные anonymous age agreements и неактивные токены.
    /// </summary>
    public class RetentionPurgeWorker : PeriodicBackgroundWorkerBase
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const string LOGGER_NAME = "EList.Services.Impl.BackgroundWorkers.RetentionPurgeWorker.";

        public RetentionPurgeWorker(
            IServiceScopeFactory scopeFactory,
            ICorrelationIdProvider correlationIdProvider)
            : base(scopeFactory, correlationIdProvider, log, LOGGER_NAME)
        {
        }

        protected override string ConfigSectionName => "retentionPurge";
        protected override string WorkerName => "RetentionPurge";

        protected override async Task ExecuteIterationAsync(IServiceProvider scopedServices, CancellationToken stoppingToken)
        {
            var methodName = $"{LOGGER_NAME}{nameof(ExecuteIterationAsync)}";
            var correlationId = scopedServices.GetRequiredService<ICorrelationIdProvider>().Get();
            var logger = new NLogLoggerWrapper(log);

            var agreementRepository = scopedServices.GetRequiredService<IAgreementRepository>();
            var authorizationRepository = scopedServices.GetRequiredService<IAuthorizationRepository>();
            var connectionProvider = scopedServices.GetRequiredService<IDataConnectionProvider>();

            await connectionProvider.StartNewTransactionAsync();
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                var agePurged = await agreementRepository.PurgeExpiredAnonymousAgeAgreementsAsync();
                stoppingToken.ThrowIfCancellationRequested();
                var tokensPurged = await authorizationRepository.PurgeInactiveTokensAsync(TimeSpan.FromDays(30));
                await connectionProvider.CommitTransactionAsync();

                logger.Info(correlationId, null, methodName,
                    $"Purged anonymous age rows={agePurged}, inactive tokens={tokensPurged}", null);
            }
            catch
            {
                await connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
