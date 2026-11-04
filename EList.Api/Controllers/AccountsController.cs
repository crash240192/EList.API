using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Events;
using EList.Models.Location;
using EList.Models.Privacy;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConfigurationManager = EList.Common.Configuration.ConfigurationManager;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/accounts")]
    [LoggerHandlerWebApiFilter]
    public class AccountsController : Controller
    {
        private readonly IAccountsService _accountsService;
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IMediaService _mediaService;
        private readonly IProfilePrivacyService _profilePrivacyService;

        public AccountsController(IAccountsService accountsService,
            IDataConnectionProvider connectionProvider,
            IMediaService mediaService,
            IProfilePrivacyService profilePrivacyService)
        {
            _accountsService = accountsService;
            _connectionProvider = connectionProvider;
            _mediaService = mediaService;
            _profilePrivacyService = profilePrivacyService;
        }

        /// <summary>
        /// Создание аккаунта
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<CommandResult> CreateAccountAsync(CreateAccountRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var configurationAllowed = false;
                if (ConfigurationManager.AppSettings.Contains("registrationAllowed"))
                    configurationAllowed = bool.Parse(ConfigurationManager.AppSettings["registrationAllowed"]);

                if (!configurationAllowed)
                    return CommandResult.Fail(ErrorCode.RegistrationForbiden, "Регистрация временно недоступна");

                var result = await _accountsService.CreateAccountAsync(request);
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
        /// Проверка уникальности логина и контакта авторизации (шаг 1 регистрации).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("checkAvailability")]
        public async Task<CommandResult> CheckRegistrationAvailabilityAsync(CheckRegistrationAvailabilityRequest request)
        {
            var configurationAllowed = false;
            if (ConfigurationManager.AppSettings.Contains("registrationAllowed"))
                configurationAllowed = bool.Parse(ConfigurationManager.AppSettings["registrationAllowed"]);

            if (!configurationAllowed)
                return CommandResult.Fail(ErrorCode.RegistrationForbiden, "Регистрация временно недоступна");

            var result = await _accountsService.CheckRegistrationAvailabilityAsync(request);
            return result;
        }

        /// <summary>
        /// Получить информацию о текущем аккаунте
        /// </summary>
        /// <returns></returns>
        [HttpGet("getData")]
        public async Task<CommandResult<Account?>> GetAccountAsync()
        {
            var result = await _accountsService.GetAccountByTokenAsync();
            return result;
        }

        /// <summary>
        /// Обновление координат пользователя
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpPost("updateLocation")]
        public async Task<CommandResult> UpdateLocationAsync(Location location)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _accountsService.UpdateLocationAsync(location.Latitude, location.Longitude);
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
        /// Получить информацию об аккаунте
        /// </summary>
        /// <returns></returns>
        [HttpGet("getData/{accountId}")]
        public async Task<CommandResult<Account?>> GetAccountAsync(Guid accountId)
        {
            var result = await _accountsService.GetAccountAsync(accountId);
            return result;
        }

        /// <summary>
        /// Lookup аккаунта по логину или GUID (для передачи билета / подарка).
        /// </summary>
        [HttpGet("lookup")]
        public async Task<CommandResult<AccountLookupResponse?>> LookupAccountAsync([FromQuery] string q)
        {
            var result = await _accountsService.LookupAccountAsync(q);
            return result;
        }

        /// <summary>
        /// Удаление (деактивация + анонимизация) текущего аккаунта
        /// </summary>
        [HttpDelete("me")]
        public async Task<CommandResult> DeleteMyAccountAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _accountsService.DeleteMyAccountAsync();
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
        /// Экспорт персональных данных текущего пользователя (JSON)
        /// </summary>
        [HttpGet("me/export")]
        public async Task<CommandResult<AccountDataExport>> ExportMyDataAsync()
        {
            var result = await _accountsService.ExportMyDataAsync();
            return result;
        }

        /// <summary>
        /// Получить настройки приватности текущего аккаунта
        /// </summary>
        [HttpGet("privacy")]
        public async Task<CommandResult<AccountPrivacySettings>> GetPrivacySettingsAsync()
        {
            var result = await _profilePrivacyService.GetMySettingsAsync();
            return result;
        }

        /// <summary>
        /// Обновить настройки приватности текущего аккаунта
        /// </summary>
        [HttpPut("privacy")]
        public async Task<CommandResult<AccountPrivacySettings>> UpdatePrivacySettingsAsync(
            UpdatePrivacySettingsRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _profilePrivacyService.UpdateMySettingsAsync(request);
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
        /// Можно ли текущему пользователю приглашать указанный аккаунт (с учётом whoCanInviteMe).
        /// </summary>
        [HttpGet("canInvite/{accountId}")]
        public async Task<CommandResult<CanInviteResult>> CanInviteAsync(Guid accountId)
        {
            var result = await _profilePrivacyService.CanInviteAsync(accountId);
            return result;
        }

        /// <summary>
        /// Массовая проверка canInvite (для модалки приглашения с события).
        /// </summary>
        [HttpPost("canInvite/batch")]
        public async Task<CommandResult<List<CanInviteResult>>> CanInviteBatchAsync(CanInviteBatchRequest request)
        {
            var result = await _profilePrivacyService.CanInviteBatchAsync(request?.AccountIds);
            return result;
        }

        /// <summary>
        /// Установить / сбросить фото шапки профиля (imageId, focusX/Y 0…100, blur).
        /// imageId = null — сброс шапки.
        /// </summary>
        [HttpPut("header")]
        public async Task<CommandResult<Account?>> SetAccountHeaderAsync([FromBody] AccountHeaderRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                var result = await _accountsService.SetAccountHeaderAsync(request);
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
        /// Список закрепов «Мои мероприятия» (сверху вниз).
        /// </summary>
        [HttpGet("myEventPins")]
        public async Task<CommandResult<MyEventPinsResponse>> GetMyEventPinsAsync()
        {
            var result = await _accountsService.GetMyEventPinsAsync();
            return result;
        }

        /// <summary>
        /// Полная замена списка закрепов «Мои мероприятия» (порядок = сверху вниз).
        /// </summary>
        [HttpPut("myEventPins")]
        public async Task<CommandResult<MyEventPinsResponse>> SetMyEventPinsAsync([FromBody] MyEventPinsRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                var result = await _accountsService.SetMyEventPinsAsync(request);
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
    }
}
