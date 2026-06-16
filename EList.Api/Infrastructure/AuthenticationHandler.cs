using EList.Common.Encryption;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NLog;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Constants = EList.Common.Constants.Constants;

namespace EList.Api.Infrastructure
{
    /// <summary>
    /// Хендлер выполнения авторизации на API контроллере
    /// </summary>
    public class AuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string ClassName = "Booking.Infrastructures.BasicAuthenticationHandler";
        private readonly Logger _currentLogger = LogManager.GetCurrentClassLogger();
        private readonly IAuthorizationService _authorizationService;
        private readonly IAccountsService _accountsService;
        private readonly IEncryptionTool _encryptionTool;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IPersonsService _personsService;

        /// <summary>
        /// Инициализация хендлера
        /// </summary>
        public AuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            [NotNull] IAuthorizationService authorizationService,
            [NotNull] IAccountsService accountsService,
            IEncryptionTool encryptionTool,
            IPersonsService personsService,
            IAccountDataHolder accountDataHolder) : base(options, logger, encoder, clock)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _encryptionTool = encryptionTool ?? throw new ArgumentNullException(nameof(encryptionTool));
            _accountsService = accountsService ?? throw new ArgumentNullException(nameof(accountsService));
            _accountDataHolder= accountDataHolder ?? throw new ArgumentNullException(nameof(accountDataHolder));
            _personsService = personsService ?? throw new ArgumentNullException(nameof(personsService));
        }

        /// <summary>
        /// Процесс проверки авторизационных данных
        /// </summary>
        /// <returns>Результат проверки</returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            CheckCorrelationIdHeader();
            var logger = _currentLogger.WithProperty("methodName", $"{ClassName}.HandleAuthenticateAsync");

            try
            {
                logger.Debug("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Get Authorization header");
                var tokenHeader = Request.Headers.ContainsKey("Authorization") ? Request.Headers["Authorization"] : StringValues.Empty;
                var jwtHeader = Request.Headers.ContainsKey("Authorization-jwt") ? Request.Headers["Authorization-jwt"] : StringValues.Empty;

                if (jwtHeader == StringValues.Empty)
                    return AuthenticateResult.Fail("Invalid Authorization-jwt Header");

                var jwtHash = _encryptionTool.CalculateStringHash(jwtHeader);

                var claims = new List<Claim> { new Claim(ClaimTypes.Hash, jwtHash) };

                if (tokenHeader != StringValues.Empty)
                    claims.Add(new Claim(ClaimTypes.PrimarySid, tokenHeader));

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                if ((Request.Path == "/api/accounts/create" && Request.Method == "POST") || (Request.Path == "/api/authorization" && Request.Method == "POST"))
                {
                    return AuthenticateResult.Success(ticket);
                }

                if (!Request.Headers.ContainsKey("Authorization"))
                {
                    logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Missing Authorization Header");
                    return AuthenticateResult.Fail("Missing Authorization Header");
                }

                var tokenIsGuid = Guid.TryParse(tokenHeader, out var tokenValue);
                if (!tokenIsGuid)
                    return AuthenticateResult.Fail("Authorization header must be Guid");

                logger.Debug($"Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Get Token by id: {tokenValue}");

                var authorizationItem = await _authorizationService.GetAuthorizationDataAsync(tokenValue);
                if (!authorizationItem.Success)
                {
                    logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Invalid Authorization Header");
                    return AuthenticateResult.Fail("Invalid Authorization Header");
                }
                if (!authorizationItem.Result.Active && Request.Path != "/api/authorization/sendActivationCode" && Request.Path != "/api/authorization/activate")
                {
                    logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Token not activated");
                    return AuthenticateResult.Fail("Token not activated");
                }

                _accountDataHolder.Token = authorizationItem.Result.Token;

                var account = await _accountsService.GetAccountByTokenAsync();
                if (!account.Result.Active)
                {
                    logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Account disabled");
                    return AuthenticateResult.Fail("Account disabled");
                }

                if (!account.Success) 
                {
                    logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Invalid Authorization Header");
                    return AuthenticateResult.Fail("Account inavailable");
                }

                _accountDataHolder.Account = account.Result;
                _accountDataHolder.PersonInfo = (await _personsService.GetPersonInfoByAccountIdAsync(account.Result.Id))?.Result;

                if (!authorizationItem.Result.Active && (Request.Path != "/api/authorization/activate" && Request.Path != "/api/authorization/sendActivationCode"))
                {
                    logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Token is not active");
                    return AuthenticateResult.Fail("Token is not active");
                }

                if (authorizationItem.Result.ClientHash != jwtHash)
                {
                    logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Token is not available for this client");
                    return AuthenticateResult.Fail("Token is not available for this client");
                }

                logger.Debug("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Success");
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Fail: Invalid Authorization Header");
                return AuthenticateResult.Fail("Invalid Authorization Header");
            }
        }


        /// <summary>
        /// Проверка наличия переданного CorrelationId идентификатора
        /// </summary>
        private void CheckCorrelationIdHeader()
        {
            var correlationIdValue = Request.Headers.ContainsKey(Constants.HttpHeaderKeys.CORRELATION_ID) ? Request.Headers[Constants.HttpHeaderKeys.CORRELATION_ID].ToString() : null;
            if (string.IsNullOrWhiteSpace(correlationIdValue))
                correlationIdValue = Request.Headers.ContainsKey("CorrelationId") ? Request.Headers["CorrelationId"].ToString() : null;

            if (string.IsNullOrWhiteSpace(correlationIdValue))
                correlationIdValue = Guid.NewGuid().ToString();

            if (Request.HttpContext.Items.ContainsKey(Constants.HttpHeaderKeys.CORRELATION_ID))
                Request.HttpContext.Items[Constants.HttpHeaderKeys.CORRELATION_ID] = correlationIdValue;
            else
                Request.HttpContext.Items.Add(new KeyValuePair<object, object>(Constants.HttpHeaderKeys.CORRELATION_ID, correlationIdValue));
        }
    }
}