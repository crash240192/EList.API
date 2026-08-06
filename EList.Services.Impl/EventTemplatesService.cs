using AutoMapper;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.EventTemplates;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class EventTemplatesService : IEventTemplatesService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.EventTemplatesService.";
        #endregion

        private readonly IEventTemplatesRepository _eventTemplatesRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IMapper _mapper;

        public EventTemplatesService(IEventTemplatesRepository eventTemplatesRepository,
            IOrganizationsRepository organizationsRepository,
            IAccountDataHolder accountDataHolder,
            ICorrelationIdProvider correlationIdProvider,
            IMapper mapper)
        {
            _eventTemplatesRepository = eventTemplatesRepository ?? throw new ArgumentNullException(nameof(eventTemplatesRepository));
            _organizationsRepository = organizationsRepository ?? throw new ArgumentNullException(nameof(organizationsRepository));
            _accountDataHolder = accountDataHolder;
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<CommandResult<Guid?>> CreateTemplateAsync(CreateEventTemplateRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateTemplateAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (string.IsNullOrWhiteSpace(request?.Name))
                return CommandResult<Guid?>.Fail(ErrorCode.IsNullOrEmpty, "Название шаблона обязательно");

            if (request.TemplateBody == null)
                return CommandResult<Guid?>.Fail(ErrorCode.IsNullOrEmpty, "Тело шаблона обязательно");

            var template = new EventTemplate
            {
                Name = request.Name.Trim(),
                TemplateBody = request.TemplateBody
            };

            if (request.OrganizationId != null)
            {
                var organization = await _organizationsRepository.GetOrganizationAsync(request.OrganizationId.Value);
                if (organization == null || !organization.Active)
                    return CommandResult<Guid?>.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{request.OrganizationId}' не найдена");

                var isOwnerOrManager = await _organizationsRepository.IsOwnerOrManagerAsync(request.OrganizationId.Value, _accountDataHolder.AccountId.Value);
                if (!isOwnerOrManager)
                    return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Недостаточно прав для создания шаблона организации");

                template.OwnerOrganizationId = request.OrganizationId;
            }
            else
            {
                template.OwnerAccountId = _accountDataHolder.AccountId;
            }

            var templateId = await _eventTemplatesRepository.CreateAsync(template);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(templateId);
        }

        public async Task<CommandResult<EventTemplateResponse?>> GetTemplateAsync(Guid templateId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetTemplateAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<EventTemplateResponse?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var template = await _eventTemplatesRepository.GetByIdAsync(templateId);
            if (template == null)
                return CommandResult<EventTemplateResponse?>.Fail(ErrorCode.EventTemplateNotFound, $"Шаблон с id='{templateId}' не найден");

            if (!await CanAccessTemplateAsync(template))
                return CommandResult<EventTemplateResponse?>.Fail(ErrorCode.AccessError, "Недостаточно прав для просмотра шаблона");

            var response = _mapper.Map<EventTemplateResponse>(template);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<EventTemplateResponse?>(response);
        }

        public async Task<CommandResult> UpdateTemplateAsync(Guid templateId, UpdateEventTemplateRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateTemplateAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var template = await _eventTemplatesRepository.GetByIdAsync(templateId);
            if (template == null)
                return CommandResult.Fail(ErrorCode.EventTemplateNotFound, $"Шаблон с id='{templateId}' не найден");

            if (!await CanManageTemplateAsync(template))
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав для изменения шаблона");

            if (!string.IsNullOrWhiteSpace(request?.Name))
                template.Name = request.Name.Trim();

            if (request?.TemplateBody != null)
                template.TemplateBody = request.TemplateBody;

            await _eventTemplatesRepository.UpdateAsync(template);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteTemplateAsync(Guid templateId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteTemplateAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var template = await _eventTemplatesRepository.GetByIdAsync(templateId);
            if (template == null)
                return CommandResult.Fail(ErrorCode.EventTemplateNotFound, $"Шаблон с id='{templateId}' не найден");

            if (!await CanManageTemplateAsync(template))
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав для удаления шаблона");

            await _eventTemplatesRepository.DeleteAsync(templateId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<EventTemplateResponse>>> SearchTemplatesAsync(EventTemplateSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SearchTemplatesAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<EventTemplateResponse>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            List<EventTemplate> templates;

            if (request?.OrganizationId != null)
            {
                var organization = await _organizationsRepository.GetOrganizationAsync(request.OrganizationId.Value);
                if (organization == null || !organization.Active)
                    return CommandResult<List<EventTemplateResponse>>.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{request.OrganizationId}' не найдена");

                var isMember = await _organizationsRepository.IsActiveMemberAsync(request.OrganizationId.Value, _accountDataHolder.AccountId.Value);
                if (!isMember)
                    return CommandResult<List<EventTemplateResponse>>.Fail(ErrorCode.AccessError, "Недостаточно прав для просмотра шаблонов организации");

                templates = await _eventTemplatesRepository.SearchByOrganizationIdAsync(request.OrganizationId.Value, request.Name);
            }
            else
            {
                templates = await _eventTemplatesRepository.SearchByAccountIdAsync(_accountDataHolder.AccountId.Value, request?.Name);
            }

            var response = templates.Select(i => _mapper.Map<EventTemplateResponse>(i)).ToList();

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<EventTemplateResponse>>(response);
        }

        private async Task<bool> CanAccessTemplateAsync(EventTemplate template)
        {
            if (_accountDataHolder.AccountId == null)
                return false;

            if (template.OwnerAccountId == _accountDataHolder.AccountId)
                return true;

            if (template.OwnerOrganizationId != null)
                return await _organizationsRepository.IsActiveMemberAsync(template.OwnerOrganizationId.Value, _accountDataHolder.AccountId.Value);

            return false;
        }

        private async Task<bool> CanManageTemplateAsync(EventTemplate template)
        {
            if (_accountDataHolder.AccountId == null)
                return false;

            if (template.OwnerAccountId == _accountDataHolder.AccountId)
                return true;

            if (template.OwnerOrganizationId != null)
                return await _organizationsRepository.IsOwnerOrManagerAsync(template.OwnerOrganizationId.Value, _accountDataHolder.AccountId.Value);

            return false;
        }
    }
}
