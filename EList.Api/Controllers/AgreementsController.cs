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

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/agreements")]
    [LoggerHandlerWebApiFilter]
    public class AgreementsController : Controller
    {
        private readonly IAgreementService _agreementService;
        private readonly IAccountsService _accountsService;
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IAccountDataHolder _accountDataHolder;

        public AgreementsController(IAgreementService agreementService,
            IAccountsService accountsService, IDataConnectionProvider connectionProvider,
            IAccountDataHolder accountDataHolder)
        {
            _agreementService = agreementService;
            _accountsService = accountsService;
            _connectionProvider = connectionProvider;
            _accountDataHolder = accountDataHolder;
        }

        /// <summary>
        /// Соглашение что анонимному пользователю есть 18+
        /// </summary>
        /// <returns></returns>Ы
        [AllowAnonymous]
        [HttpGet("age/anonymous/agree")]
        public async Task<CommandResult> SaveAnonymousAgeAgreementAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _agreementService.SaveAnonymousAgeAgreementAsync();
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
        /// Проставил ли пользователь галочку "мне есть 18"
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("age/anonymous/get")]
        public async Task<CommandResult> GetAnonymousAgeAgreementAsync()
        {
            var result = await _agreementService.GetAnonymousAgeAgreementAsync();
            return result;
        }

        /// <summary>
        /// Проверяет, подписал ли пользователь соглашение указанного типа
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>        
        [HttpGet("checkUserAgreement/{documentType}")]        
        public async Task<CommandResult> DoesUserAgreedWithLatestDocumentVersion(DocumentType documentType)
        {
            var result = await _agreementService.DoesUserAgreedWithLatestDocumentVersion(documentType);
            return result;

        }

        /// <summary>
        /// Отметка о согласии пользователя с соглашением
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>
        [HttpGet("agree/{documentType}")]
        public async Task<CommandResult> SaveUserAgreementAsync(DocumentType documentType)
        {
            var result = await _agreementService.SaveUserAgreementAsync(documentType);
            if (!result.Success)
                await _connectionProvider.RollbackTransactionAsync();
            return result;
        }

        /// <summary>
        /// Проверяет, подписала ли организация соглашение указанного типа
        /// </summary>
        [HttpGet("checkOrganizationAgreement/{organizationId}/{documentType}")]
        public async Task<CommandResult> DoesOrganizationAgreedWithLatestDocumentVersion(Guid organizationId, DocumentType documentType)
        {
            var result = await _agreementService.DoesOrganizationAgreedWithLatestDocumentVersion(organizationId, documentType);
            return result;

        }

        /// <summary>
        /// Отметка о согласии организации с соглашением
        /// </summary>
        [HttpGet("agree/organization/{organizationId}/{documentType}")]
        public async Task<CommandResult> SaveOrganizationAgreementAsync(Guid organizationId, DocumentType documentType)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _agreementService.SaveOrganizationAgreementAsync(organizationId, documentType);
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

        [HttpPost("documents/add")]
        public async Task<CommandResult> AddNewDocumentAsync(DocumentRequest request)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(ErrorCode.AccessError, "Добавление юридических документов доступно только администраторам площадки");
            var result = await _agreementService.AddNewDocumentAsync(request);
            if (!result.Success)
                await _connectionProvider.RollbackTransactionAsync();
            return result;
        }

        /// <summary>
        /// Возвращает список документов соглашений последней версии
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("documents/last")]
        public async Task<CommandResult<List<Document>>> GetLatestDocumentsAsync()
        {
            var result = await _agreementService.GetLatestDocumentsAsync();
            
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
            var result = await _agreementService.GetLatestDocumentAsync(documentType);

            return result;
        }

        /// <summary>
        /// Отзыв согласия на обработку ПДн: углублённое удаление/анонимизация аккаунта.
        /// </summary>
        [HttpPost("withdraw")]
        public async Task<CommandResult> WithdrawConsentAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _accountsService.DeleteMyAccountAsync();
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
