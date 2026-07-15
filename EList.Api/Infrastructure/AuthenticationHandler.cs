using EList.Common.Encryption;
using EList.Models;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
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

        private static readonly List<string> AnonymousMethods = new()
        {
            "/api/accounts/getdata/*",
            "/api/contacts/getaccountcontacts/*",

            "/api/contacts/contacttypes/get/*",
            "/api/contacts/contacttypes/getall",

            "/api/conversations/get/*",
            "/api/conversations/byevent/*",
            "/api/conversations/messages/byconversationid/*",
            "/api/conversations/messages/replies/*",

            "/api/eventorganizators/getbyeventid/*",

            "/api/events/eventcategories/get/*",
            "/api/events/eventcategories/getall",
            "/api/events/eventtypes/get/*",
            "/api/events/eventtypes/bycategoryid/*",
            "/api/events/eventtypes/getall",
            "/api/events/eventparameters/byevent/*",
            "/api/events/get/*",
            "/api/events/search",
            "/api/events/search/short",

            "/api/media/albums/get/*",
            "/api/media/albums/byaccount/*",
            "/api/media/albums/filesbyalbumid/*",
            "/api/media/albums/byevent/*",
            "/api/media/albums/byevents",
            "/api/media/account/avatars/get*",
            "/api/media/account/avatar/get*",
            "/api/media/organization/avatars/get*",
            "/api/media/organization/avatar/get*",

            "/api/participations/eventparticipants",
            "/api/participations/blacklist/get/*",
            "/api/participations/whitelist/get/*",

            "/api/persons/get*",

            "/api/rating/events/getrating",
            "/api/rating/organizators/*",

            "/api/subscriptions/getsubscriptions*",
            "/api/subscriptions/getsubscribers*",

            "/api/agreements/age/anonymous/agree",
            "/api/agreements/age/anonymous/get"
        };

        private readonly Logger _currentLogger = LogManager.GetCurrentClassLogger();
        private readonly IAuthorizationService _authorizationService;
        private readonly IAccountsService _accountsService;
        private readonly IEncryptionTool _encryptionTool;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IPersonsService _personsService;

        private bool IsAnonymousMethod // Не обязательны ни токен, ни jwt
        {
            get
            {
                var path = Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                return AnonymousMethods.Contains(path)
                    || AnonymousMethods
                        .Where(i => i.EndsWith('*'))
                        .Any(i => path.Contains(i[..^1], StringComparison.Ordinal));
            }
        }

        private bool IsRegistrationFlow => // Jwt обязателен
            (Request.Path == "/api/accounts/create" && Request.Method == "POST");

        private bool IsAuthorizationFlow => // Jwt обязателен
            (Request.Path == "/api/authorization" && Request.Method == "POST");

        private bool IsActivationFlow => // Jwt и токен обязателен
            Request.Path == "/api/authorization/sendActivationCode"
            || Request.Path == "/api/authorization/activate";

        private bool IsResetPasswordFlow => // Jwt обязателен
            Request.Path == "/api/authorization/forgotPassword"
            || Request.Path == "/api/authorization/verifyResetCode"
            || Request.Path == "/api/authorization/resetPassword";

        //private bool IsMainFlow => !IsRegistrationFlow && !IsAuthorizationFlow && !IsActivationFlow && !IsResetPasswordFlow;

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
            _accountDataHolder = accountDataHolder ?? throw new ArgumentNullException(nameof(accountDataHolder));
            _personsService = personsService ?? throw new ArgumentNullException(nameof(personsService));
        }

        /// <summary>
        /// Процесс проверки авторизационных данных
        /// </summary>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            CheckCorrelationIdHeader();
            var logger = _currentLogger.WithProperty("methodName", $"{ClassName}.HandleAuthenticateAsync");

            try
            {
                logger.Debug("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Get Authorization header");

                var hasToken = Request.Headers.TryGetValue("Authorization", out var tokenHeader)
                    && !StringValues.IsNullOrEmpty(tokenHeader);
                var hasJwt = Request.Headers.TryGetValue("Authorization-jwt", out var jwtHeader)
                    && !StringValues.IsNullOrEmpty(jwtHeader);

                #region Для регистраци и сброса пароля ждём только jwt
                if (IsRegistrationFlow || IsResetPasswordFlow)
                {
                    if (!hasJwt)
                        return AuthenticateResult.Fail("Missing Authorization-jwt header");

                    var clientHash = GetClientHash(jwtHeader.ToString());
                    _accountDataHolder.ClientHash = clientHash;
                    _accountDataHolder.Jwt = jwtHeader.ToString();
                    _accountDataHolder.ClientInfo = GetClientInfo();

                    return AuthenticateResult.Success(CreateTicket(jwtHeader.ToString(), hasToken ? tokenHeader.ToString() : null));
                }
                #endregion

                //Дальше идут все остальные случаи проверки доступа

                #region проверка наличия токена и jwt в заголовках
                //jwt не нужен только для анонимных методов, всем остальным - обязателен
                if (!hasJwt)
                {
                    if (!IsAnonymousMethod)
                        return AuthenticateResult.Fail("Missing Authorization-jwt Header");
                }
                else
                {
                    var clientHash = GetClientHash(jwtHeader.ToString());
                    _accountDataHolder.ClientHash = clientHash;
                    _accountDataHolder.Jwt = jwtHeader.ToString();
                    _accountDataHolder.ClientInfo = GetClientInfo();
                }

                //Токен не обязателен для анонимных и не нужен для авторизации
                if (!hasToken)
                {
                    if (!IsAuthorizationFlow && !IsRegistrationFlow && !IsResetPasswordFlow && !IsAnonymousMethod)
                        return AuthenticateResult.Fail("Missing Authorization Header");
                }
                else
                {
                    if (!Guid.TryParse(tokenHeader, out var tokenValue))
                    {
                        if (!IsAuthorizationFlow && !IsRegistrationFlow && !IsResetPasswordFlow && !IsAnonymousMethod)
                            return AuthenticateResult.Fail("Authorization header must be Guid");
                    }
                    else
                    {
                        _accountDataHolder.Token = tokenValue;
                    }
                }
                #endregion

                var ticket = CreateTicket(hasJwt ? jwtHeader.ToString() : null,
                    hasToken ? tokenHeader.ToString() : null);

                if (_accountDataHolder.Token != null && !IsAuthorizationFlow && !IsRegistrationFlow && !IsResetPasswordFlow)
                {
                    var tokenValidationResult = await ValidateTokenAsync(_accountDataHolder.Token.Value, ticket);

                    if (!tokenValidationResult.Succeeded)
                        return tokenValidationResult;

                    var account = await _accountsService.GetAccountByTokenAsync();
                    if (!account.Success || account.Result == null)
                    {
                        if (!IsAnonymousMethod)
                        {
                            logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Invalid Authorization Header");
                            return AuthenticateResult.Fail("Account inavailable");
                        }
                    }

                    if (!account.Result.Active)
                    {
                        if (!IsAnonymousMethod)
                        {
                            logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Account disabled");
                            return AuthenticateResult.Fail("Account disabled");
                        }
                    }

                    _accountDataHolder.Account = account.Result;
                    _accountDataHolder.PersonInfo = (await _personsService.GetPersonInfoByAccountIdAsync(account.Result.Id))?.Result;
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

        private async Task<AuthenticateResult> ValidateTokenAsync(Guid token, AuthenticationTicket ticket)
        {
            var logger = _currentLogger.WithProperty("methodName", $"{ClassName}.ValidateTokenAsync");

            logger.Debug($"Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Get Token by id: {_accountDataHolder.Token}");
            var dbToken = await _authorizationService.GetAuthorizationDataAsync(token);

            // Если токен не найден и это не активация и не анонимный метод, то ошибка авторизации
            if (!dbToken.Success && !IsAnonymousMethod && !IsActivationFlow)
            {
                logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Invalid Authorization Header");
                return AuthenticateResult.Fail($"Invalid Authorization Header: {dbToken.Message}");
            }

            // Если токен не активен и это не активация и не анонимный метод, то ошибка авторизации
            if ((!dbToken.Result?.Active ?? false) && !IsAnonymousMethod && !IsActivationFlow)
            {
                logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Token not activated");
                return AuthenticateResult.Fail("Token not activated");
            }

            // Если хэши клиента в токене и в запросе не совпадают и это не анонимный метод, то ошибка авторизации
            if (((dbToken.Result?.ClientHash ?? null) != _accountDataHolder.ClientHash) && !IsAnonymousMethod)
            {
                logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Token is not available for this client");
                return AuthenticateResult.Fail("Token is not available for this client");
            }

            return AuthenticateResult.Success(ticket);
        }

        private string GetClientHash(string jwtHeader)
        {
            var jwtHash = _encryptionTool.CalculateStringHash(jwtHeader);
            var platform = Request.Headers["X-Client-Platform"].FirstOrDefault() ?? "unknown";
            var appVersion = Request.Headers["X-App-Version"].FirstOrDefault() ?? "unknown";

            return _encryptionTool.CalculateStringHash($"{jwtHash}|{platform}|{appVersion}");
        }

        private string GetClientInfo()
        {
            var clientInfo = (ClientInfo)Request.HttpContext.Items["ClientInfo"];

            return $"{clientInfo.IP}|{clientInfo.Timezone}|{clientInfo.AcceptLanguage}";
        }

        private AuthenticationTicket CreateAnonymousTicket()
        {
            var identity = new ClaimsIdentity(Array.Empty<Claim>(), Scheme.Name);
            return new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        }

        private AuthenticationTicket CreateTicket(string? jwt, string? token)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrWhiteSpace(jwt))
                claims.Add(new Claim(ClaimTypes.Hash, _encryptionTool.CalculateStringHash(jwt)));
            if (!string.IsNullOrWhiteSpace(token))
                claims.Add(new Claim(ClaimTypes.PrimarySid, token));

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
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
