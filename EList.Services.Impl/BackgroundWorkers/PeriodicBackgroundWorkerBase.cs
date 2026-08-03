using EList.Common.Configuration;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace EList.Services.Impl.BackgroundWorkers
{
    /// <summary>
    /// Базовый шаблон периодического фонового воркера на <see cref="BackgroundService"/>.
    /// Создаёт DI-scope на каждую итерацию и корректно обрабатывает остановку.
    /// </summary>
    public abstract class PeriodicBackgroundWorkerBase : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly ILoggerWrapper _logger;
        private readonly string _loggerName;

        private readonly object _sync = new();
        private bool _active;
        private int _processIntervalMinutes;
        private int _itemDelayMilliseconds;

        protected PeriodicBackgroundWorkerBase(
            IServiceScopeFactory scopeFactory,
            ICorrelationIdProvider correlationIdProvider,
            ILogger nlogLogger,
            string loggerName)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _logger = new NLogLoggerWrapper(nlogLogger);
            _loggerName = loggerName;

            LoadOptionsFromConfiguration();
        }

        /// <summary>
        /// Имя секции конфигурации (например, debtCollector).
        /// </summary>
        protected abstract string ConfigSectionName { get; }

        /// <summary>
        /// Человекочитаемое имя воркера для логов.
        /// </summary>
        protected abstract string WorkerName { get; }

        public bool Active
        {
            get { lock (_sync) return _active; }
        }

        protected int ItemDelayMilliseconds
        {
            get { lock (_sync) return _itemDelayMilliseconds; }
        }

        public void ManualStart()
        {
            lock (_sync)
            {
                _active = true;
            }

            _logger.Info(_correlationIdProvider.Get(), null, $"{_loggerName}{nameof(ManualStart)}",
                $"{WorkerName} enabled manually", null);
        }

        public void ManualStop()
        {
            lock (_sync)
            {
                _active = false;
            }

            _logger.Info(_correlationIdProvider.Get(), null, $"{_loggerName}{nameof(ManualStop)}",
                $"{WorkerName} disabled manually", null);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var methodName = $"{_loggerName}{nameof(ExecuteAsync)}";
            _logger.Info(_correlationIdProvider.Get(), null, methodName,
                $"{WorkerName} hosted service started (active={Active}, intervalMinutes={GetIntervalMinutes()})", null);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (Active)
                {
                    try
                    {
                        await RunIterationAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(_correlationIdProvider.Get(), null, methodName,
                            $"{WorkerName} iteration failed: {ex.Message}", null, ex);
                    }
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(GetIntervalMinutes()), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.Info(_correlationIdProvider.Get(), null, methodName, $"{WorkerName} hosted service stopped", null);
        }

        private async Task RunIterationAsync(CancellationToken stoppingToken)
        {
            var methodName = $"{_loggerName}{nameof(RunIterationAsync)}";
            var correlationId = _correlationIdProvider.Get();
            _logger.Debug(correlationId, null, methodName, $"{WorkerName} iteration started", null);

            using var scope = _scopeFactory.CreateScope();
            await ExecuteIterationAsync(scope.ServiceProvider, stoppingToken);

            _logger.Debug(correlationId, null, methodName, $"{WorkerName} iteration finished", null);
        }

        /// <summary>
        /// Бизнес-логика одной итерации. Резолвить scoped-зависимости из <paramref name="scopedServices"/>.
        /// </summary>
        protected abstract Task ExecuteIterationAsync(IServiceProvider scopedServices, CancellationToken stoppingToken);

        protected async Task DelayBetweenItemsAsync(CancellationToken stoppingToken)
        {
            var delay = ItemDelayMilliseconds;
            if (delay > 0)
                await Task.Delay(delay, stoppingToken);
        }

        private void LoadOptionsFromConfiguration()
        {
            var options = new BackgroundWorkerOptions();

            if (ConfigurationManager.AppSettings.Contains($"{ConfigSectionName}:active")
                && bool.TryParse(ConfigurationManager.AppSettings[$"{ConfigSectionName}:active"], out var active))
            {
                options.Active = active;
            }

            if (ConfigurationManager.AppSettings.Contains($"{ConfigSectionName}:processIntervalMinutes")
                && int.TryParse(ConfigurationManager.AppSettings[$"{ConfigSectionName}:processIntervalMinutes"], out var interval)
                && interval > 0)
            {
                options.ProcessIntervalMinutes = interval;
            }

            if (ConfigurationManager.AppSettings.Contains($"{ConfigSectionName}:itemDelayMilliseconds")
                && int.TryParse(ConfigurationManager.AppSettings[$"{ConfigSectionName}:itemDelayMilliseconds"], out var itemDelay)
                && itemDelay >= 0)
            {
                options.ItemDelayMilliseconds = itemDelay;
            }

            lock (_sync)
            {
                _active = options.Active;
                _processIntervalMinutes = options.ProcessIntervalMinutes;
                _itemDelayMilliseconds = options.ItemDelayMilliseconds;
            }
        }

        private int GetIntervalMinutes()
        {
            lock (_sync)
            {
                return _processIntervalMinutes <= 0 ? 10 : _processIntervalMinutes;
            }
        }
    }
}
