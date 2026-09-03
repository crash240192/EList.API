using System.Text.Json;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using ConfigurationManager = EList.Common.Configuration.ConfigurationManager;

namespace EList.Api.Middleware
{
    /// <summary>
    /// Блокирует API для авторизованных пользователей, пока они не приняли
    /// актуальные версии Policy / Consent / Agreement.
    /// </summary>
    public class ReConsentMiddleware
    {
        private static readonly DocumentType[] RequiredUserDocuments =
        {
            DocumentType.Policy,
            DocumentType.Consent,
            DocumentType.Agreement
        };

        private static readonly string[] AlwaysAllowedPathPrefixes =
        {
            "/health",
            "/version",
            "/swagger",
            "/api/agreements",
            "/api/authorization",
            "/api/accounts/create",
            "/api/accounts/me",
            "/eList/health",
            "/eList/version",
            "/eList/swagger",
            "/eList/api/agreements",
            "/eList/api/authorization",
            "/eList/api/accounts/create",
            "/eList/api/accounts/me"
        };

        private readonly RequestDelegate _next;

        public ReConsentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IAccountDataHolder accountDataHolder,
            IAgreementRepository agreementRepository)
        {
            if (!IsEnforcementEnabled())
            {
                await _next(context);
                return;
            }

            if (accountDataHolder.AccountId == null
                || IsAllowedPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var missing = new List<string>();
            foreach (var documentType in RequiredUserDocuments)
            {
                var latest = await agreementRepository.GetLatestDocumentAsync(documentType);
                if (latest == null)
                    continue; // документ ещё не загружен админом — не блокируем

                var agreed = await agreementRepository.DoesUserAgreedWithLatestDocumentVersion(
                    accountDataHolder.AccountId.Value, documentType);
                if (!agreed)
                    missing.Add(documentType.ToString());
            }

            if (missing.Count == 0)
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new
            {
                errorCode = ErrorCode.AgreementNotFound,
                success = false,
                message = "Необходимо принять обновлённые юридические документы, прежде чем продолжить пользоваться сервисом",
                missingDocuments = missing
            });
            await context.Response.WriteAsync(body);
        }

        private static bool IsEnforcementEnabled()
        {
            if (!ConfigurationManager.AppSettings.Contains("features:reConsentEnforcementEnabled"))
                return true;

            return !bool.TryParse(
                       ConfigurationManager.AppSettings["features:reConsentEnforcementEnabled"],
                       out var enabled)
                   || enabled;
        }

        private static bool IsAllowedPath(PathString path)
        {
            var value = path.Value ?? string.Empty;
            return AlwaysAllowedPathPrefixes.Any(prefix =>
                value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
