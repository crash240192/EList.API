using EList.Common.Configuration;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.FilestorageClient;
using EList.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace EList.Services.Impl.BackgroundWorkers
{
    /// <summary>
    /// Periodic orphan reconciler: ask filestorage for old Active files, delete those with no refs in elist DB.
    /// Complements sync deletes on album/message cleanup.
    /// </summary>
    public class OrphanFileGcWorker : PeriodicBackgroundWorkerBase
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const string LOGGER_NAME = "EList.Services.Impl.BackgroundWorkers.OrphanFileGcWorker.";

        private readonly int _olderThanDays;
        private readonly int _batchSize;

        public OrphanFileGcWorker(
            IServiceScopeFactory scopeFactory,
            ICorrelationIdProvider correlationIdProvider)
            : base(scopeFactory, correlationIdProvider, log, LOGGER_NAME)
        {
            _olderThanDays = ReadInt("orphanFileGc:olderThanDays", 7);
            _batchSize = ReadInt("orphanFileGc:batchSize", 100);
        }

        protected override string ConfigSectionName => "orphanFileGc";
        protected override string WorkerName => "OrphanFileGc";

        protected override async Task ExecuteIterationAsync(IServiceProvider scopedServices, CancellationToken stoppingToken)
        {
            var methodName = $"{LOGGER_NAME}{nameof(ExecuteIterationAsync)}";
            var correlationId = scopedServices.GetRequiredService<ICorrelationIdProvider>().Get();
            var logger = new NLogLoggerWrapper(log);

            var filestorage = scopedServices.GetRequiredService<IFilestorageClient>();
            var mediaRepository = scopedServices.GetRequiredService<IMediaRepository>();

            var candidates = await filestorage.GetGcCandidateIdsAsync(_olderThanDays, _batchSize);
            if (!candidates.Success || candidates.Result == null || candidates.Result.Count == 0)
            {
                logger.Debug(correlationId, null, methodName,
                    $"No GC candidates (success={candidates.Success}, msg={candidates.Message})", null);
                return;
            }

            stoppingToken.ThrowIfCancellationRequested();
            var orphans = await mediaRepository.FilterUnreferencedFileIdsAsync(candidates.Result);
            if (orphans.Count == 0)
            {
                logger.Debug(correlationId, null, methodName,
                    $"All {candidates.Result.Count} candidates still referenced", null);
                return;
            }

            var deleted = 0;
            foreach (var fileId in orphans)
            {
                stoppingToken.ThrowIfCancellationRequested();
                try
                {
                    var result = await filestorage.DeleteFileAsServiceAsync(fileId);
                    if (result.Success)
                        deleted++;
                    else
                        logger.Warn(correlationId, null, methodName,
                            $"GC delete failed for {fileId}: {result.Message}", null);
                }
                catch (Exception ex)
                {
                    logger.Warn(correlationId, null, methodName,
                        $"GC delete exception for {fileId}: {ex.Message}", null);
                }

                await DelayBetweenItemsAsync(stoppingToken);
            }

            logger.Info(correlationId, null, methodName,
                $"Orphan GC: candidates={candidates.Result.Count}, unreferenced={orphans.Count}, deleted={deleted}", null);
        }

        private static int ReadInt(string key, int fallback)
        {
            if (ConfigurationManager.AppSettings.Contains(key)
                && int.TryParse(ConfigurationManager.AppSettings[key], out var value)
                && value > 0)
                return value;
            return fallback;
        }
    }
}
