using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Authorization = EList.Models.Authorization.Authorization;
using IAuthorizationService = EList.Services.Interfaces.IAuthorizationService;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/authorization")]
    [LoggerHandlerWebApiFilter]
    public class AuthorizationController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IDataConnectionProvider _connectionProvider;

        public AuthorizationController(IAuthorizationService authorizationService, IDataConnectionProvider connectionProvider)
        {
            _authorizationService = authorizationService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<CommandResult<AuthorizationResponse>> AuthorizeAsync(AuthorizationRequest request)
        {
            var result = await _authorizationService.AuthorizeAsync(request.Login, request.Password);
            return result;

        }

        /// <summary>
        /// Активация зарегистрированного токена
        /// </summary>
        /// <param name="activationKey"></param>
        /// <returns></returns>
        [HttpGet("activate")]
        public async Task<CommandResult> ActivateTokenAsync(string activationKey)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _authorizationService.ActivateTokenAsync(activationKey);

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
        /// Проверка авторизации
        /// </summary>
        /// <returns></returns>
        [HttpGet("check")]
        public async Task<CommandResult> CheckAuthorizationAsync()
        {
            var result = CommandResult.OK;
            return result;
        }

        /// <summary>
        /// Выслать код активации
        /// </summary>
        /// <returns></returns>
        [HttpGet("sendActivationCode")]
        public async Task<CommandResult<string>> SendActivationCodeAsync()
        {
            var result = await _authorizationService.SendActivationCodeAsync();
            return result;

        }

        /*public async Task<CommandResult> DeactivateTokenAsync(Guid token)
        {
            var result = await _authorizationService.AuthorizeAsync(login, passwordHash, clientHash);
            return result;

        }*/

        /*
        [HttpPost("create")]
        public async Task<CommandResult<Guid>> CreateTokenAsync(Guid accountId)
        {
            var clientHash = this.GetClientHash();
            var result = await _authorizationService.CreateTokenAsync(accountId, clientHash);
            return result;

        }*/

        /*public async Task<CommandResult<Authorization?>> GetAuthorizationDataAsync()
        {
            var token = this.GetToken();
            var result = await _authorizationService.GetAuthorizationDataAsync(token);
            return result;

        }*/

        /// <summary>
        /// Отдаёт токен для текущего устрйства (не особо
        /// </summary>
        /// <returns></returns>
        //[AllowAnonymous]
        /*[HttpPost("getData")]
        public async Task<CommandResult<AuthorizationResponse?>> GetAuthorizationDataAsync()
        {
            var result = await _authorizationService.GetAuthorizationDataAsync(clientHash);
            return result;

        }*/

        /// <summary>
        /// Изменение пароля
        /// </summary>
        /// <returns></returns>
        [HttpPost("changePassword")]
        public async Task<CommandResult> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _authorizationService.ChangePasswordAsync(request);
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
        /// Отправка одноразового кода для смены пароля
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        [HttpPost("forgotPassword")]
        public async Task<CommandResult> ForgotPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _authorizationService.ForgotPasswordAsync(request?.Login);
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
        /// Валидация кода сброса пароля
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("verifyResetCode")]
        public async Task<CommandResult> VerifyResetPasswordAsync(VerifyResetPasswordRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _authorizationService.VerifyResetPasswordAsync(request?.Login, request?.Code);
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
        /// Сброс пароля для механизма "не помню пароль"
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("resetPassword")]
        public async Task<CommandResult<AuthorizationResponse>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _authorizationService.ResetPasswordAsync(request);
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
