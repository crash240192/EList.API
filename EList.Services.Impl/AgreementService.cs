using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Encryption;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.UserAgreements;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl
{
    public class AgreementService : IAgreementService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.AgreementService.";
        #endregion

        private readonly IAgreementRepository _agreementRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IEncryptionTool _encryptionTool;

        public AgreementService(ICorrelationIdProvider correlationIdProvider,
            IAgreementRepository agreementRepository,
            IOrganizationsRepository organizationsRepository,
            IAccountDataHolder accountDataHolder,
            IEncryptionTool encryptionTool)
        {
            _agreementRepository = agreementRepository;
            _organizationsRepository = organizationsRepository;
            _correlationIdProvider = correlationIdProvider;
            _accountDataHolder = accountDataHolder;
            _encryptionTool = encryptionTool;
        }

        public async Task<CommandResult<AnonymousAgeAgreement>> GetAnonymousAgeAgreementAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAnonymousAgeAgreementAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var agreement = await _agreementRepository.GetAnonymousAgeAgreementAsync(_accountDataHolder.Jwt);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            if (agreement != null)
                return new CommandResult<AnonymousAgeAgreement>(agreement);
            else
                return CommandResult<AnonymousAgeAgreement>.Fail(ErrorCode.AgreementNotFound, "Пользователь не подтвердил что ему есть 18");
        }

        public async Task<CommandResult> SaveAnonymousAgeAgreementAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SaveAnonymousAgeAgreementAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            await _agreementRepository.SaveAnonymousAgeAgreement(_accountDataHolder.Jwt, _accountDataHolder.ClientInfo);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }



        public async Task<CommandResult> DoesUserAgreedWithLatestDocumentVersion(DocumentType documentType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DoesUserAgreedWithLatestDocumentVersion)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            var checkResult = await _agreementRepository.DoesUserAgreedWithLatestDocumentVersion(_accountDataHolder.AccountId.Value, documentType);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            var result = checkResult switch
            {
                true => CommandResult.OK,
                false => CommandResult.Fail(ErrorCode.AgreementNotFound, "Соглашение с пользователем отсутствует")
            };

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return result;
        }

        public async Task<CommandResult> SaveUserAgreementAsync(DocumentType documentType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SaveUserAgreementAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            var lastDocument = await _agreementRepository.GetLatestDocumentAsync(documentType);

            if (lastDocument == null)
                return CommandResult.Fail(ErrorCode.AgreementDocumentNotFound, "Документ для соглашения отсутствует в базе");

            await _agreementRepository.SaveUserAgreementAsync(_accountDataHolder.AccountId.Value, lastDocument.Id);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DoesOrganizationAgreedWithLatestDocumentVersion(Guid organizationId, DocumentType documentType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DoesOrganizationAgreedWithLatestDocumentVersion)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            var isOwnerOrManager = await _organizationsRepository.IsOwnerOrManagerAsync(organizationId, _accountDataHolder.AccountId.Value);
            if (!isOwnerOrManager)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав для просмотра соглашений организации");

            var checkResult = await _agreementRepository.DoesOrganizationAgreedWithLatestDocumentVersion(organizationId, documentType);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            var result = checkResult switch
            {
                true => CommandResult.OK,
                false => CommandResult.Fail(ErrorCode.AgreementNotFound, "Соглашение с организацией отсутствует")
            };

            return result;
        }

        public async Task<CommandResult> SaveOrganizationAgreementAsync(Guid organizationId, DocumentType documentType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SaveOrganizationAgreementAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            var isOwnerOrManager = await _organizationsRepository.IsOwnerOrManagerAsync(organizationId, _accountDataHolder.AccountId.Value);
            if (!isOwnerOrManager)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав для принятия соглашения организации");

            var lastDocument = await _agreementRepository.GetLatestDocumentAsync(documentType);

            if (lastDocument == null)
                return CommandResult.Fail(ErrorCode.AgreementDocumentNotFound, "Документ для соглашения отсутствует в базе");

            await _agreementRepository.SaveOrganizationAgreementAsync(organizationId, lastDocument.Id);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> AddNewDocumentAsync(DocumentRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AddNewDocumentAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (string.IsNullOrWhiteSpace(request.Header))
                return CommandResult.Fail(ErrorCode.DocumentHeaderIsEmpty, "Заголовок документа не должен быть пустым");

            if (string.IsNullOrWhiteSpace(request.Text))
                return CommandResult.Fail(ErrorCode.DocumentIsEmpty, "Текст документа не должен быть пустым");

            var latestDocument = await _agreementRepository.GetLatestDocumentAsync(request.Type);

            var hash = _encryptionTool.CalculateStringHash(request.Text);

            var versionesult = FormatAndVerifyVersion(request.Version, latestDocument?.Version);
            if (!versionesult.Success)
                return versionesult;

            await _agreementRepository.AddNewDocumentAsync(new Document
            {
                Text = request.Text,
                Hash = hash,
                CreationDate = DateTime.UtcNow,
                Header = request.Header,
                Type = request.Type,
                Version = versionesult.Result
            });

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<Document>>> GetLatestDocumentsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetLatestDocumentsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _agreementRepository.GetLatestDocumentsAsync();
            if (result == null)
                return CommandResult<List<Document>>.Fail(ErrorCode.AgreementDocumentNotFound, "Документы для соглашений отсутствуют в базе");


            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<Document>>(result);
        }

        public async Task<CommandResult<Document>> GetLatestDocumentAsync(DocumentType type)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetLatestDocumentAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _agreementRepository.GetLatestDocumentAsync(type);
            if (result == null)
                return CommandResult<Document>.Fail(ErrorCode.AgreementDocumentNotFound, "Документ для соглашений отсутствуют в базе");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Document>(result);
        }


        private CommandResult<string> FormatAndVerifyVersion(string newVersion, string existingVersion)
        {
            existingVersion = string.IsNullOrWhiteSpace(existingVersion) ? "0.0.0" : existingVersion;
            var existingVersionValues = existingVersion.Split('.').Select(i => Int32.Parse(i)).ToList();

            newVersion = string.IsNullOrWhiteSpace(newVersion) ? "1.0.0" : newVersion;
            var newVersionValues = newVersion.Split('.').Select(i => Int32.Parse(i)).ToList();

            if (newVersionValues.Count != 3)
                return CommandResult<string>.Fail(ErrorCode.InvalidVersion, "Формат версии должен быть 'x.x.x'");

            for (int i = 0; i < existingVersionValues.Count; i++)
            {
                if (newVersionValues[i] > existingVersionValues[i])
                    return new CommandResult<string>(string.Join(".", newVersionValues));
                if (newVersionValues[i] < existingVersionValues[i])
                    return CommandResult<string>.Fail(ErrorCode.InvalidVersion, "Недопустимо понижение версии");
            }

            return CommandResult<string>.Fail(ErrorCode.InvalidVersion, "Новая версия должна быть выше текущей");
        }
    }
}
