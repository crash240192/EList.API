using AutoMapper;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.Organizations;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class OrganizationsService : IOrganizationsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.OrganizationsService.";
        #endregion

        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IWalletsRepository _walletsRepository;
        private readonly IOrganizationRegistryClient _organizationRegistryClient;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IMapper _mapper;
        private readonly INotificationsService _notificationsService;

        public OrganizationsService(IOrganizationsRepository organizationsRepository,
            IAccountsRepository accountsRepository,
            IWalletsRepository walletsRepository,
            IOrganizationRegistryClient organizationRegistryClient,
            IAccountDataHolder accountDataHolder,
            ICorrelationIdProvider correlationIdProvider,
            IMapper mapper,
            INotificationsService notificationsService)
        {
            _organizationsRepository = organizationsRepository ?? throw new ArgumentNullException(nameof(organizationsRepository));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _walletsRepository = walletsRepository ?? throw new ArgumentNullException(nameof(walletsRepository));
            _organizationRegistryClient = organizationRegistryClient ?? throw new ArgumentNullException(nameof(organizationRegistryClient));
            _accountDataHolder = accountDataHolder;
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
        }

        public async Task<CommandResult<Guid?>> CreateOrganizationAsync(OrganizationRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateOrganizationAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (string.IsNullOrWhiteSpace(request?.Name))
                return CommandResult<Guid?>.Fail(ErrorCode.IsNullOrEmpty, "Название организации обязательно");

            var walletId = await _walletsRepository.CreateWalletAsync();

            var organization = new Organization
            {
                Name = request.Name.Trim(),
                Description = request.Description,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Active = true,
                WalletId = walletId,
                CreatedByAccountId = _accountDataHolder.AccountId,
                VerificationStatus = OrganizationVerificationStatus.Unverified,
                CanSellTickets = false
            };

            var organizationId = await _organizationsRepository.CreateOrganizationAsync(organization);

            await _organizationsRepository.AddMemberAsync(new OrganizationMember
            {
                OrganizationId = organizationId,
                AccountId = _accountDataHolder.AccountId.Value,
                Role = OrganizationMemberRole.Owner,
                Active = true
            });

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(organizationId);
        }

        public async Task<CommandResult<OrganizationResponse?>> GetOrganizationAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizationAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var organization = await _organizationsRepository.GetOrganizationFullAsync(organizationId);
            if (organization == null)
                return CommandResult<OrganizationResponse?>.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            var isMember = _accountDataHolder.AccountId != null
                && await _organizationsRepository.IsActiveMemberAsync(organizationId, _accountDataHolder.AccountId.Value);

            if (!organization.Active && !isMember)
                return CommandResult<OrganizationResponse?>.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            var response = _mapper.Map<OrganizationResponse>(organization);

            if (!isMember)
            {
                response.Members = null;
                response.Legal = null;
                response.Payout = null;
            }
            else
            {
                var isOwnerOrManager = await _organizationsRepository.IsOwnerOrManagerAsync(organizationId, _accountDataHolder.AccountId!.Value);
                if (!isOwnerOrManager)
                {
                    response.Legal = null;
                    response.Payout = null;
                }
                else
                {
                    response.Legal = organization.Legal != null ? _mapper.Map<OrganizationLegalResponse>(organization.Legal) : null;
                    response.Payout = organization.Payout != null ? _mapper.Map<OrganizationPayoutResponse>(organization.Payout) : null;
                }

                response.Members = organization.Members != null
                    ? _mapper.Map<List<OrganizationMemberResponse>>(organization.Members)
                    : null;
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<OrganizationResponse?>(response);
        }

        public async Task<CommandResult<List<OrganizationResponse>?>> GetMyOrganizationsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyOrganizationsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<OrganizationResponse>?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var organizations = await _organizationsRepository.GetOrganizationsByAccountIdAsync(_accountDataHolder.AccountId.Value);
            var response = _mapper.Map<List<OrganizationResponse>>(organizations);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<OrganizationResponse>?>(response);
        }

        public async Task<CommandResult<List<OrganizationResponse>?>> GetUserOrganizationsAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyOrganizationsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var organizations = await _organizationsRepository.GetOrganizationsByAccountIdAsync(accountId);
            var response = _mapper.Map<List<OrganizationResponse>>(organizations);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<OrganizationResponse>?>(response);
        }

        public async Task<CommandResult> UpdateOrganizationAsync(Guid organizationId, OrganizationRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateOrganizationAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerOrManagerAsync(organizationId);
            if (accessError != null)
                return accessError;

            if (string.IsNullOrWhiteSpace(request?.Name))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Название организации обязательно");

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            organization!.Name = request.Name.Trim();
            organization.Description = request.Description;
            organization.Address = request.Address;
            organization.Latitude = request.Latitude;
            organization.Longitude = request.Longitude;

            await _organizationsRepository.UpdateOrganizationAsync(organization);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> SetOrganizationActiveAsync(Guid organizationId, bool active)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetOrganizationActiveAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerAsync(organizationId);
            if (accessError != null)
                return accessError;

            await _organizationsRepository.SetOrganizationActiveAsync(organizationId, active);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<OrganizationMemberResponse>?>> GetMembersAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMembersAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureActiveMemberAsync(organizationId);
            if (accessError != null)
                return CommandResult<List<OrganizationMemberResponse>?>.Fail(accessError.ErrorCode, accessError.Message);

            var members = await _organizationsRepository.GetMembersByOrganizationIdAsync(organizationId);
            var response = _mapper.Map<List<OrganizationMemberResponse>>(members);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<OrganizationMemberResponse>?>(response);
        }

        public async Task<CommandResult<Guid?>> AddManagerAsync(Guid organizationId, AddOrganizationMemberRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AddManagerAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerAsync(organizationId);
            if (accessError != null)
                return CommandResult<Guid?>.Fail(accessError.ErrorCode, accessError.Message);

            if (request.AccountId == Guid.Empty)
                return CommandResult<Guid?>.Fail(ErrorCode.IsNullOrEmpty, "Не указан аккаунт менеджера");

            var account = await _accountsRepository.GetAccountAsync(request.AccountId);
            if (account == null)
                return CommandResult<Guid?>.Fail(ErrorCode.AccountNotFound, $"Аккаунт с id='{request.AccountId}' не найден");

            var existingMember = await _organizationsRepository.GetMemberAsync(organizationId, request.AccountId);
            if (existingMember != null)
            {
                if (existingMember.Active)
                    return CommandResult<Guid?>.Fail(ErrorCode.OrganizationMemberAlreadyExists, "Пользователь уже является участником организации");

                await _organizationsRepository.SetMemberActiveAsync(organizationId, request.AccountId, true);
                if (existingMember.Role != OrganizationMemberRole.Owner)
                    await _organizationsRepository.UpdateMemberRoleAsync(organizationId, request.AccountId, OrganizationMemberRole.Manager);

                await _notificationsService.NotifyOrganizationMemberAddedAsync(organizationId, request.AccountId);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return new CommandResult<Guid?>(existingMember.Id);
            }

            var memberId = await _organizationsRepository.AddMemberAsync(new OrganizationMember
            {
                OrganizationId = organizationId,
                AccountId = request.AccountId,
                Role = OrganizationMemberRole.Manager,
                Active = true,
                InvitedBy = _accountDataHolder.AccountId
            });

            await _notificationsService.NotifyOrganizationMemberAddedAsync(organizationId, request.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(memberId);
        }

        public async Task<CommandResult> RemoveMemberAsync(Guid organizationId, Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(RemoveMemberAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerAsync(organizationId);
            if (accessError != null)
                return accessError;

            var member = await _organizationsRepository.GetMemberAsync(organizationId, accountId);
            if (member == null)
                return CommandResult.Fail(ErrorCode.OrganizationMemberNotFound, "Участник организации не найден");

            if (member.Role == OrganizationMemberRole.Owner)
                return CommandResult.Fail(ErrorCode.AccessError, "Нельзя удалить владельца организации. Сначала передайте владение");

            await _organizationsRepository.RemoveMemberAsync(organizationId, accountId);
            await _notificationsService.NotifyOrganizationMemberRemovedAsync(organizationId, accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> SetMemberActiveAsync(Guid organizationId, Guid accountId, bool active)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetMemberActiveAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerAsync(organizationId);
            if (accessError != null)
                return accessError;

            var member = await _organizationsRepository.GetMemberAsync(organizationId, accountId);
            if (member == null)
                return CommandResult.Fail(ErrorCode.OrganizationMemberNotFound, "Участник организации не найден");

            if (member.Role == OrganizationMemberRole.Owner)
                return CommandResult.Fail(ErrorCode.AccessError, "Нельзя деактивировать владельца организации");

            await _organizationsRepository.SetMemberActiveAsync(organizationId, accountId, active);

            if (active)
                await _notificationsService.NotifyOrganizationMemberAddedAsync(organizationId, accountId);
            else
                await _notificationsService.NotifyOrganizationMemberDeactivatedAsync(organizationId, accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> TransferOwnershipAsync(Guid organizationId, TransferOwnershipRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(TransferOwnershipAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerAsync(organizationId);
            if (accessError != null)
                return accessError;

            if (request.NewOwnerAccountId == Guid.Empty)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Не указан новый владелец");

            if (request.NewOwnerAccountId == _accountDataHolder.AccountId)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Нельзя передать владение самому себе");

            var account = await _accountsRepository.GetAccountAsync(request.NewOwnerAccountId);
            if (account == null)
                return CommandResult.Fail(ErrorCode.AccountNotFound, $"Аккаунт с id='{request.NewOwnerAccountId}' не найден");

            var previousOwnerAccountId = _accountDataHolder.AccountId!.Value;
            await _organizationsRepository.TransferOwnershipAsync(
                organizationId,
                previousOwnerAccountId,
                request.NewOwnerAccountId);

            await _notificationsService.NotifyOrganizationOwnershipTransferredAsync(
                organizationId,
                request.NewOwnerAccountId,
                previousOwnerAccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> UpsertLegalAsync(Guid organizationId, OrganizationLegalRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpsertLegalAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerAsync(organizationId);
            if (accessError != null)
                return accessError;

            if (!Enum.IsDefined(typeof(OrganizationLegalForm), request.LegalForm))
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректная юридическая форма");

            if (string.IsNullOrWhiteSpace(request.Inn))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "ИНН обязателен");

            var legal = _mapper.Map<OrganizationLegal>(request);
            legal.OrganizationId = organizationId;
            legal.VerifiedAt = null;

            await _organizationsRepository.UpsertLegalAsync(legal);

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization?.VerificationStatus == OrganizationVerificationStatus.Verified
                || organization?.VerificationStatus == OrganizationVerificationStatus.Pending)
            {
                if (organization.CanSellTickets)
                    await _organizationsRepository.SetCanSellTicketsAsync(organizationId, false);
                await _organizationsRepository.SetVerificationStatusAsync(organizationId, OrganizationVerificationStatus.Unverified);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<OrganizationLegalResponse?>> GetLegalAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetLegalAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerOrManagerAsync(organizationId);
            if (accessError != null)
                return CommandResult<OrganizationLegalResponse?>.Fail(accessError.ErrorCode, accessError.Message);

            var legal = await _organizationsRepository.GetLegalAsync(organizationId);
            var response = _mapper.Map<OrganizationLegalResponse>(legal);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<OrganizationLegalResponse?>(response);
        }

        public async Task<CommandResult> UpsertPayoutAsync(Guid organizationId, OrganizationPayoutRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpsertPayoutAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerAsync(organizationId);
            if (accessError != null)
                return accessError;

            if (string.IsNullOrWhiteSpace(request.BankAccount) || string.IsNullOrWhiteSpace(request.Bik))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Расчётный счёт и БИК обязательны");

            var existing = await _organizationsRepository.GetPayoutAsync(organizationId);
            var payout = existing ?? new OrganizationPayout { OrganizationId = organizationId };

            payout.BankAccount = request.BankAccount;
            payout.Bik = request.Bik;
            payout.BankName = request.BankName;
            payout.TaxRegime = request.TaxRegime;
            payout.UpdatedBy = _accountDataHolder.AccountId;

            await _organizationsRepository.UpsertPayoutAsync(payout);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<OrganizationPayoutResponse?>> GetPayoutAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetPayoutAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerOrManagerAsync(organizationId);
            if (accessError != null)
                return CommandResult<OrganizationPayoutResponse?>.Fail(accessError.ErrorCode, accessError.Message);

            var payout = await _organizationsRepository.GetPayoutAsync(organizationId);
            var response = _mapper.Map<OrganizationPayoutResponse>(payout);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<OrganizationPayoutResponse?>(response);
        }

        public async Task<CommandResult> SubmitVerificationAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SubmitVerificationAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerAsync(organizationId);
            if (accessError != null)
                return accessError;

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            if (organization.VerificationStatus == OrganizationVerificationStatus.Pending)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Заявка на верификацию уже отправлена");

            if (organization.VerificationStatus == OrganizationVerificationStatus.Verified)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Организация уже верифицирована");

            var legal = await _organizationsRepository.GetLegalAsync(organizationId);
            if (legal == null || string.IsNullOrWhiteSpace(legal.Inn) || string.IsNullOrWhiteSpace(legal.HeadName))
                return CommandResult.Fail(ErrorCode.OrganizationLegalDataRequired, "Сначала заполните юридические реквизиты организации");

            var payout = await _organizationsRepository.GetPayoutAsync(organizationId);
            if (payout == null || string.IsNullOrWhiteSpace(payout.BankAccount) || string.IsNullOrWhiteSpace(payout.Bik))
                return CommandResult.Fail(ErrorCode.OrganizationPayoutDataRequired, "Сначала заполните платёжные реквизиты организации");

            await _organizationsRepository.SetVerificationStatusAsync(organizationId, OrganizationVerificationStatus.Pending);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> SetCanSellTicketsAsync(Guid organizationId, bool canSellTickets)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetCanSellTicketsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await EnsureOwnerAsync(organizationId);
            if (accessError != null)
                return accessError;

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            if (canSellTickets && organization.VerificationStatus != OrganizationVerificationStatus.Verified)
                return CommandResult.Fail(ErrorCode.OrganizationNotVerified, "Продажа билетов доступна только верифицированным организациям");

            await _organizationsRepository.SetCanSellTicketsAsync(organizationId, canSellTickets);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<OrganizationRegistryParty?>> LookupByInnAsync(string inn)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(LookupByInnAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<OrganizationRegistryParty?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (string.IsNullOrWhiteSpace(inn))
                return CommandResult<OrganizationRegistryParty?>.Fail(ErrorCode.IsNullOrEmpty, "ИНН обязателен");

            OrganizationRegistryParty? party;
            try
            {
                party = await _organizationRegistryClient.FindByInnAsync(inn);
            }
            catch (Exception ex)
            {
                logger.Warn(correlationId, null, methodName, $"Lookup failed: {ex.Message}", null);
                return CommandResult<OrganizationRegistryParty?>.Fail(ErrorCode.InternalError, "Сервис поиска организаций временно недоступен");
            }

            if (party == null)
                return CommandResult<OrganizationRegistryParty?>.Fail(ErrorCode.OrganizationNotFound, "Организация/ИП не найдена по указанному ИНН");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<OrganizationRegistryParty?>(party);
        }

        private async Task<CommandResult?> EnsureOwnerAsync(Guid organizationId)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            var isOwner = await _organizationsRepository.IsOwnerAsync(organizationId, _accountDataHolder.AccountId.Value);
            if (!isOwner)
                return CommandResult.Fail(ErrorCode.AccessError, "Действие доступно только владельцу организации");

            return null;
        }

        private async Task<CommandResult?> EnsureOwnerOrManagerAsync(Guid organizationId)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            var isOwnerOrManager = await _organizationsRepository.IsOwnerOrManagerAsync(organizationId, _accountDataHolder.AccountId.Value);
            if (!isOwnerOrManager)
                return CommandResult.Fail(ErrorCode.AccessError, "Действие доступно только владельцу или менеджеру организации");

            return null;
        }

        private async Task<CommandResult?> EnsureActiveMemberAsync(Guid organizationId)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            var isMember = await _organizationsRepository.IsActiveMemberAsync(organizationId, _accountDataHolder.AccountId.Value);
            if (!isMember)
                return CommandResult.Fail(ErrorCode.AccessError, "Действие доступно только участникам организации");

            return null;
        }
    }
}
