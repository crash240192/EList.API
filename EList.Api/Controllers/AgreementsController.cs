using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.DbDataProvider.Interfaces;
using EList.Models.Enums;
using EList.Models.UserAgreements;
using EList.Services.Impl;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/agreements")]
    [LoggerHandlerWebApiFilter]
    public class AgreementsController : Controller
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.AgreementsController.";
        #endregion

        private readonly IAgreementService _agreementService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public AgreementsController(IAgreementService agreementService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
            _agreementService = agreementService;
        }


        /// <summary>
        /// Соглашение что анонимному пользователю есть 18+
        /// </summary>
        /// <returns></returns>Ы
        [AllowAnonymous]
        [HttpGet("age/anonymous/agree")]
        public async Task<CommandResult> SaveAnonymousAgeAgreementAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SaveAnonymousAgeAgreementAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _agreementService.SaveAnonymousAgeAgreementAsync();
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Проставил ли пользователь галочку "мне есть 18"
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("age/anonymous/get")]
        public async Task<CommandResult> GetAnonymousAgeAgreementAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAnonymousAgeAgreementAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _agreementService.GetAnonymousAgeAgreementAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }


        /// <summary>
        /// Проверяет, подписал ли пользователь соглашение указанного типа
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>        
        [HttpGet("checkUserAgreement/{documentType}")]        
        public async Task<CommandResult> DoesUserAgreedWithLatestDocumentVersion(DocumentType documentType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DoesUserAgreedWithLatestDocumentVersion)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _agreementService.DoesUserAgreedWithLatestDocumentVersion(documentType);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Отметка о согласии пользователя с соглашением
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("agree/{documentType}")]
        public async Task<CommandResult> SaveUserAgreementAsync(DocumentType documentType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SaveUserAgreementAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _agreementService.SaveUserAgreementAsync(documentType);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.OK;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Проверяет, подписала ли организация соглашение указанного типа
        /// </summary>
        [HttpGet("checkOrganizationAgreement/{organizationId}/{documentType}")]
        public async Task<CommandResult> DoesOrganizationAgreedWithLatestDocumentVersion(Guid organizationId, DocumentType documentType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DoesOrganizationAgreedWithLatestDocumentVersion)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _agreementService.DoesOrganizationAgreedWithLatestDocumentVersion(organizationId, documentType);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Отметка о согласии организации с соглашением
        /// </summary>
        [HttpGet("agree/organization/{organizationId}/{documentType}")]
        public async Task<CommandResult> SaveOrganizationAgreementAsync(Guid organizationId, DocumentType documentType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SaveOrganizationAgreementAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _agreementService.SaveOrganizationAgreementAsync(organizationId, documentType);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        [HttpPost("documents/add")]
        public async Task<CommandResult> AddNewDocumentAsync(DocumentRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AddNewDocumentAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _agreementService.AddNewDocumentAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.OK;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Возвращает список документов соглашений последней версии
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("documents/last")]
        public async Task<CommandResult<List<Document>>> GetLatestDocumentsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetLatestDocumentsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _agreementService.GetLatestDocumentsAsync();
            
            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return result;
        }

        /// <summary>
        /// Возвращает документ соглашения последней версии указанного типа
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("documents/last/{documentType}")]
        public async Task<CommandResult<Document>> GetLatestDocumentAsync(DocumentType documentType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetLatestDocumentAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _agreementService.GetLatestDocumentAsync(documentType);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return result;
        }
    }
}
