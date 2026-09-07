using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Models.Enums;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace EList.Services.Impl.BackgroundWorkers
{
    /// <summary>
    /// Фоновая верификация организаций, ожидающих проверки в гос. реестре.
    /// </summary>
    public class OrganizationVerificationWorker : PeriodicBackgroundWorkerBase
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const string LOGGER_NAME = "EList.Services.Impl.BackgroundWorkers.OrganizationVerificationWorker.";

        public OrganizationVerificationWorker(
            IServiceScopeFactory scopeFactory,
            ICorrelationIdProvider correlationIdProvider)
            : base(scopeFactory, correlationIdProvider, log, LOGGER_NAME)
        {
        }

        protected override string ConfigSectionName => "organizationVerification";
        protected override string WorkerName => "OrganizationVerification";

        protected override async Task ExecuteIterationAsync(IServiceProvider scopedServices, CancellationToken stoppingToken)
        {
            var methodName = $"{LOGGER_NAME}{nameof(ExecuteIterationAsync)}";
            var correlationId = scopedServices.GetRequiredService<ICorrelationIdProvider>().Get();
            var logger = new NLogLoggerWrapper(log);

            var organizationsRepository = scopedServices.GetRequiredService<IOrganizationsRepository>();
            var registryClient = scopedServices.GetRequiredService<IOrganizationRegistryClient>();
            var notificationsService = scopedServices.GetRequiredService<INotificationsService>();

            var pending = await organizationsRepository.GetPendingVerificationOrganizationsAsync();
            if (pending == null || pending.Count == 0)
            {
                logger.Debug(correlationId, null, methodName, "No pending organization verifications", null);
                return;
            }

            logger.Info(correlationId, null, methodName, $"Processing {pending.Count} pending verification(s)", null);

            foreach (var organization in pending)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    await ProcessOrganizationAsync(
                        organizationsRepository,
                        registryClient,
                        notificationsService,
                        organization,
                        correlationId,
                        logger,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.Error(correlationId, null, methodName,
                        $"Failed to verify organization '{organization.Id}': {ex.Message}", null, ex);
                }

                await DelayBetweenItemsAsync(stoppingToken);
            }
        }

        private static async Task ProcessOrganizationAsync(
            IOrganizationsRepository organizationsRepository,
            IOrganizationRegistryClient registryClient,
            INotificationsService notificationsService,
            Models.Organizations.Organization organization,
            string correlationId,
            ILoggerWrapper logger,
            CancellationToken stoppingToken)
        {
            var methodName = $"{LOGGER_NAME}{nameof(ProcessOrganizationAsync)}";

            var legal = organization.Legal ?? await organizationsRepository.GetLegalAsync(organization.Id);
            if (legal == null)
            {
                await organizationsRepository.SetVerificationStatusAsync(
                    organization.Id,
                    OrganizationVerificationStatus.Rejected,
                    "Юридические реквизиты отсутствуют");
                await notificationsService.NotifyOrganizationVerificationRejectedAsync(
                    organization.Id,
                    "Юридические реквизиты отсутствуют");
                logger.Info(correlationId, null, methodName,
                    $"Organization '{organization.Id}' rejected: legal data missing", null);
                return;
            }

            var checkResult = await registryClient.CheckOrganizationAsync(legal, organization.Name, stoppingToken);

            switch (checkResult.Outcome)
            {
                case OrganizationRegistryCheckOutcome.Verified:
                    await organizationsRepository.SetVerificationStatusAsync(
                        organization.Id,
                        OrganizationVerificationStatus.Verified);
                    await notificationsService.NotifyOrganizationVerificationApprovedAsync(organization.Id);
                    logger.Info(correlationId, null, methodName,
                        $"Organization '{organization.Id}' verified" +
                        (string.IsNullOrWhiteSpace(checkResult.OfficialName) ? string.Empty : $": {checkResult.OfficialName}"),
                        null);
                    break;

                case OrganizationRegistryCheckOutcome.Rejected:
                    await organizationsRepository.SetVerificationStatusAsync(
                        organization.Id,
                        OrganizationVerificationStatus.Rejected,
                        checkResult.Message);
                    await notificationsService.NotifyOrganizationVerificationRejectedAsync(
                        organization.Id,
                        checkResult.Message);
                    logger.Info(correlationId, null, methodName,
                        $"Organization '{organization.Id}' rejected: {checkResult.Message}", null);
                    break;

                case OrganizationRegistryCheckOutcome.Unavailable:
                    logger.Info(correlationId, null, methodName,
                        $"Organization '{organization.Id}' verification deferred: {checkResult.Message}", null);
                    break;
            }
        }
    }
}
