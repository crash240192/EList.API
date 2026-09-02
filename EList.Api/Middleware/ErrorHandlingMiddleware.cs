using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Support;
using Newtonsoft.Json;
using NLog;
using System.Net;
using System.Text;
using ILogger = NLog.ILogger;

namespace EList.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        #region NLog
        private static ILogger log = LogManager.GetCurrentClassLogger();
        private static ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.ErrorHandlingMiddleware.";
        #endregion

        private readonly RequestDelegate next;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IHostEnvironment _environment;

        public ErrorHandlingMiddleware(
            RequestDelegate next,
            ICorrelationIdProvider correlationIdProvider,
            IHostEnvironment environment)
        {
            this.next = next;
            _correlationIdProvider = correlationIdProvider;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = _correlationIdProvider.Get();
            var METHOD_NAME = LOGGER_NAME + nameof(InvokeAsync);

            logger.Debug(correlationId, null, METHOD_NAME, null, $"{nameof(InvokeAsync)} method has started");

            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                // Полная цепочка в лог (тип + message на каждом уровне + stack у exception object).
                ExceptionLogger.LogException(
                    logger,
                    correlationId,
                    METHOD_NAME,
                    $"Unhandled exception while processing {context.Request.Method} {context.Request.Path}",
                    TimeSpan.Zero,
                    exception);

                await HandleExceptionAsync(context, exception);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = (int)HttpStatusCode.InternalServerError;
            var contentType = "application/json";
            var isDevelopment = _environment.IsDevelopment();

            // В prod клиенту — безопасное сообщение без stack / внутренних деталей.
            // В Development — полная цепочка сообщений + stack для отладки.
            object bodyPayload = isDevelopment
                ? new
                {
                    errorCode = ErrorCode.InternalError,
                    success = false,
                    message = FormatExceptionChain(exception),
                    stackTrace = exception.ToString()
                }
                : new
                {
                    errorCode = ErrorCode.InternalError,
                    success = false,
                    message = "Внутренняя ошибка сервера. Обратитесь в поддержку и укажите correlation id.",
                    correlationId = _correlationIdProvider.Get()
                };

            string body = JsonConvert.SerializeObject(bodyPayload);
            context.Response.ContentType = contentType;
            context.Response.StatusCode = code;
            return context.Response.WriteAsync(body);
        }

        /// <summary>
        /// Outer → inner: сначала корневая ошибка, затем причины.
        /// Удобнее читать, чем только deepest InnerException.
        /// </summary>
        private static string FormatExceptionChain(Exception exception)
        {
            var sb = new StringBuilder();
            var current = exception;
            var level = 0;
            while (current != null)
            {
                if (level == 0)
                    sb.AppendLine($"{current.GetType().Name}: {current.Message}");
                else
                    sb.AppendLine($"  caused by [{level}] {current.GetType().Name}: {current.Message}");

                current = current.InnerException;
                level++;
            }

            return sb.ToString().TrimEnd();
        }
    }
}
