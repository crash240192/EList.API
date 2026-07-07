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
        };

        private readonly Logger _currentLogger = LogManager.GetCurrentClassLogger();
        private readonly IAuthorizationService _authorizationService;
        private readonly IAccountsService _accountsService;
        private readonly IEncryptionTool _encryptionTool;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IPersonsService _personsService;

        private bool IsAnonymousMethod
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

        private bool IsRegistrationFlow =>
            (Request.Path == "/api/accounts/create" && Request.Method == "POST")
            || (Request.Path == "/api/authorization" && Request.Method == "POST");

        private bool IsActivationFlow =>
            Request.Path == "/api/authorization/sendActivationCode"
            || Request.Path == "/api/authorization/activate";

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

                var hasAuthorization = Request.Headers.TryGetValue("Authorization", out var tokenHeader)
                    && !StringValues.IsNullOrEmpty(tokenHeader);
                var hasJwt = Request.Headers.TryGetValue("Authorization-jwt", out var jwtHeader)
                    && !StringValues.IsNullOrEmpty(jwtHeader);

                if (IsRegistrationFlow)
                {
                    if (!hasJwt)
                        return AuthenticateResult.Fail("Invalid Authorization-jwt Header");

                    return AuthenticateResult.Success(CreateTicket(jwtHeader.ToString(), hasAuthorization ? tokenHeader.ToString() : null));
                }

                if (!hasAuthorization && !hasJwt)
                {
                    if (!IsAnonymousMethod)
                        return AuthenticateResult.Fail("Missing Authorization headers");

                    ClearAccountData();
                    return AuthenticateResult.Success(CreateAnonymousTicket());
                }

                var ticket = CreateTicket(
                    hasJwt ? jwtHeader.ToString() : null,
                    hasAuthorization ? tokenHeader.ToString() : null);

                if (!hasJwt)
                {
                    if (!IsAnonymousMethod)
                        return AuthenticateResult.Fail("Invalid Authorization-jwt Header");

                    ClearAccountData();
                    return AuthenticateResult.Success(ticket);
                }

                if (!hasAuthorization)
                {
                    if (!IsAnonymousMethod)
                        return AuthenticateResult.Fail("Missing Authorization Header");

                    ClearAccountData();
                    return AuthenticateResult.Success(ticket);
                }

                if (!Guid.TryParse(tokenHeader, out var tokenValue))
                {
                    if (!IsAnonymousMethod)
                        return AuthenticateResult.Fail("Authorization header must be Guid");

                    ClearAccountData();
                    return AuthenticateResult.Success(ticket);
                }

                logger.Debug($"Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Get Token by id: {tokenValue}");

                var authorizationItem = await _authorizationService.GetAuthorizationDataAsync(tokenValue);
                if (!authorizationItem.Success)
                {
                    if (!IsAnonymousMethod)
                    {
                        logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Invalid Authorization Header");
                        return AuthenticateResult.Fail("Invalid Authorization Header");
                    }

                    ClearAccountData();
                    return AuthenticateResult.Success(ticket);
                }

                if (!authorizationItem.Result.Active && !IsActivationFlow)
                {
                    if (!IsAnonymousMethod)
                    {
                        logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Token not activated");
                        return AuthenticateResult.Fail("Token not activated");
                    }

                    ClearAccountData();
                    return AuthenticateResult.Success(ticket);
                }

                var jwtHash = _encryptionTool.CalculateStringHash(jwtHeader);
                if (authorizationItem.Result.ClientHash != jwtHash)
                {
                    if (!IsAnonymousMethod)
                    {
                        logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Token is not available for this client");
                        return AuthenticateResult.Fail("Token is not available for this client");
                    }

                    ClearAccountData();
                    return AuthenticateResult.Success(ticket);
                }

                _accountDataHolder.Token = authorizationItem.Result.Token;
                _accountDataHolder.Jwt = jwtHeader;

                var account = await _accountsService.GetAccountByTokenAsync();
                if (!account.Success || account.Result == null)
                {
                    if (!IsAnonymousMethod)
                    {
                        logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Invalid Authorization Header");
                        return AuthenticateResult.Fail("Account inavailable");
                    }

                    ClearAccountData();
                    return AuthenticateResult.Success(ticket);
                }

                if (!account.Result.Active)
                {
                    if (!IsAnonymousMethod)
                    {
                        logger.Error("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Account disabled");
                        return AuthenticateResult.Fail("Account disabled");
                    }

                    ClearAccountData();
                    return AuthenticateResult.Success(ticket);
                }

                _accountDataHolder.Account = account.Result;
                _accountDataHolder.PersonInfo = (await _personsService.GetPersonInfoByAccountIdAsync(account.Result.Id))?.Result;

                logger.Debug("Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Success");
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Start BasicAuthenticationHandler 'HandleAuthenticateAsync' method - Fail: Invalid Authorization Header");
                return AuthenticateResult.Fail("Invalid Authorization Header");
            }
        }

        private AuthenticationTicket CreateAnonymousTicket()
        {
            var identity = new ClaimsIdentity(Array.Empty<Claim>(), Scheme.Name);
            return new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        }

        private AuthenticationTicket CreateTicket(string? jwt, string? token)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(jwt))
                claims.Add(new Claim(ClaimTypes.Hash, _encryptionTool.CalculateStringHash(jwt)));
            if (!string.IsNullOrEmpty(token))
                claims.Add(new Claim(ClaimTypes.PrimarySid, token));

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        }

        private void ClearAccountData()
        {
            _accountDataHolder.Token = null;
            _accountDataHolder.Jwt = null;
            _accountDataHolder.Account = null;
            _accountDataHolder.PersonInfo = null;
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
