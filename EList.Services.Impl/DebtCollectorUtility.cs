using EList.Common.Configuration;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using FluentScheduler;
//using Microsoft.Extensions.Hosting;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EList.Services.Impl
{
    public class DebtCollectorUtility : IDebtCollectorUtility// IHostedService, IDisposable
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "TMEList.Services.Impl.DebtCollectorUtility.";

        private int _processIntervalMinutes;
        
        private readonly IWalletsRepository _walletsRepository;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        
        private bool _active;
        private bool _isStarted;
        public bool Active { get { return _active; } }

        public DebtCollectorUtility(IWalletsRepository walletsRepository,
            ICorrelationIdProvider correlationIdProvider) 
        {
            if (ConfigurationManager.AppSettings.Contains("debtCollector:processIntervalMinutes"))
                Int32.TryParse(ConfigurationManager.AppSettings["debtCollector:processIntervalMinutes"]?.ToString(), out _processIntervalMinutes);
            else
                _processIntervalMinutes = 60;


            if (ConfigurationManager.AppSettings.Contains("debtCollector:active"))
                _active = bool.Parse(ConfigurationManager.AppSettings["debtCollector:active"]);
            else
                _active = true;

            _walletsRepository = walletsRepository;
            _correlationIdProvider= correlationIdProvider;
        }

        public void ManualStart()
        {
            _active = true;
            Start();
        }

        public void ManualStop()
        {
            _active = false;
            Stop();
        }


        public void Start()
        {
            var methodName = $"{LOGGER_NAME}{nameof(Start)}";
            var correlationId = _correlationIdProvider.Get();

            try
            {
                logger.Info(correlationId, null, methodName, null, "Start method started", null);

                if (!_active)
                    return;

                if (_isStarted)
                    throw new InvalidOperationException("BackgroundUploader is already started");

                _isStarted = true;

                var reg = new Registry();
                var schedule = reg.Schedule(ChargeWalletsAsync);
                schedule.ToRunEvery(_processIntervalMinutes).Minutes();
                JobManager.Initialize(reg);
                JobManager.Start();

                logger.Info(correlationId, null, methodName, "Start method finished", null);
            }
            catch (Exception ex)
            {
                logger.Error(correlationId, null, methodName, $"Failed to start BackgroundUploader: {ex.Message}", null, ex);
            }
        }

        public void Stop()
        {
            var methodName = $"{LOGGER_NAME}{nameof(Stop)}";
            var correlationId = _correlationIdProvider.Get();

            try
            {
                logger.Info(correlationId, null, methodName, null, "Stop method started", null);

                JobManager.Stop();
                _isStarted = false;

                logger.Info(correlationId, null, methodName, "Stop method finished", null);
            }
            catch (Exception ex)
            {
                logger.Error(correlationId, null, methodName, $"Failed to stop BackgroundUploader: {ex.Message}", null, ex);
            }
        }


        private async void ChargeWalletsAsync()
        {
            var methodName = $"{LOGGER_NAME}{nameof(ChargeWalletsAsync)}";
            var correlationId = _correlationIdProvider.Get();
            try
            {
                var wallets = await _walletsRepository.GetOverdueWalletsAsync();
                if (wallets != null)
                {
                    foreach (var wallet in wallets)
                    {
                        await _walletsRepository.ChargeByTariffAsync(wallet.Id);
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(correlationId, null, methodName, $"Failed to stop BackgroundUploader: {ex.Message}", null, ex);
            }
        }
    }
}
