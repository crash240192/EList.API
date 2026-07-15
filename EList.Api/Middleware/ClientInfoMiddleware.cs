using EList.Common.CorrelationId;
using EList.Models;

namespace EList.Api.Middleware
{
    public class ClientInfoMiddleware
    {
        private readonly RequestDelegate next;

        public ClientInfoMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientInfo = new ClientInfo
            {
                IP = context.Connection.RemoteIpAddress?.ToString(),
                Port = context.Connection.RemotePort,
                Protocol = context.Request.Protocol,
                Method = context.Request.Method,
                Url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
                Path = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                Platform = context.Request.Headers["X-Client-Platform"].FirstOrDefault() ?? "unknown",
                AppVersion = context.Request.Headers["X-App-Version"].FirstOrDefault() ?? "unknown",
                Referer = context.Request.Headers["Referer"].FirstOrDefault(),
                AcceptLanguage = context.Request.Headers["Accept-Language"].FirstOrDefault(),
                CorrelationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString(),
                // ... заполните остальные поля
            };

            // Сохраните в HttpContext.Items для использования в контроллерах
            context.Items["ClientInfo"] = clientInfo;

            // Логируйте или сохраняйте в БД
            await next(context);
        }
    }
}
