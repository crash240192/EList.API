using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.PlatformRoles;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class AccountPlatformRolesService : IAccountPlatformRolesService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.AccountPlatformRolesService.";
        #endregion

        private readonly IAccountPlatformRolesRepository _rolesRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly ICorrelationIdProvider _correlationIdProvider;

        public AccountPlatformRolesService(
            IAccountPlatformRolesRepository rolesRepository,
            IAccountsRepository accountsRepository,
            IAccountDataHolder accountDataHolder,
            ICorrelationIdProvider correlationIdProvider)
        {
            _rolesRepository = rolesRepository ?? throw new ArgumentNullException(nameof(rolesRepository));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _accountDataHolder = accountDataHolder;
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
        }

        public async Task<PlatformRole?> ResolveActiveRoleAsync(Guid accountId)
        {
            var role = await _rolesRepository.GetByAccountIdAsync(accountId, onlyActive: true);
            return role?.Role;
        }

        public async Task<CommandResult<AccountPlatformRole?>> GetMyRoleAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyRoleAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<AccountPlatformRole?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var role = await _rolesRepository.GetByAccountIdAsync(_accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<AccountPlatformRole?>(role);
        }

        public async Task<CommandResult<AccountPlatformRole?>> GetByAccountIdAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetByAccountIdAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult<AccountPlatformRole?>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var role = await _rolesRepository.GetByAccountIdAsync(accountId, onlyActive: false);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<AccountPlatformRole?>(role);
        }

        public async Task<CommandResult<List<AccountPlatformRole>>> GetAllAsync(PlatformRole? role = null, bool onlyActive = true)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAllAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult<List<AccountPlatformRole>>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var result = await _rolesRepository.GetAllAsync(role, onlyActive);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<List<AccountPlatformRole>>(result);
        }

        public async Task<CommandResult<Guid?>> AssignRoleAsync(AssignPlatformRoleRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AssignRoleAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            if (!Enum.IsDefined(typeof(PlatformRole), request.Role))
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue, "Некорректная роль");

            if (request.Role == PlatformRole.Superuser && !_accountDataHolder.IsSuperuser)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Назначить superuser может только superuser");

            var account = await _accountsRepository.GetAccountAsync(request.AccountId);
            if (account == null)
                return CommandResult<Guid?>.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

            var id = await _rolesRepository.AssignRoleAsync(
                request.AccountId,
                request.Role,
                _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(id);
        }

        public async Task<CommandResult> SetActiveAsync(Guid accountId, bool active)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetActiveAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var existing = await _rolesRepository.GetByAccountIdAsync(accountId, onlyActive: false);
            if (existing == null)
                return CommandResult.Fail(ErrorCode.PlatformRoleNotFound, "Роль площадки не найдена");

            if (existing.Role == PlatformRole.Superuser && !_accountDataHolder.IsSuperuser)
                return CommandResult.Fail(ErrorCode.AccessError, "Изменять superuser может только superuser");

            await _rolesRepository.SetActiveAsync(accountId, active);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteRoleAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteRoleAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var existing = await _rolesRepository.GetByAccountIdAsync(accountId, onlyActive: false);
            if (existing == null)
                return CommandResult.Fail(ErrorCode.PlatformRoleNotFound, "Роль площадки не найдена");

            if (existing.Role == PlatformRole.Superuser && !_accountDataHolder.IsSuperuser)
                return CommandResult.Fail(ErrorCode.AccessError, "Удалять superuser может только superuser");

            await _rolesRepository.DeleteAsync(accountId);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
