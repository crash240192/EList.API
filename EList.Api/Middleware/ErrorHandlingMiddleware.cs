using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Support;
using EList.DbDataProvider.Interfaces;
using Newtonsoft.Json;
using NLog;
using System.Net;
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

        public ErrorHandlingMiddleware(RequestDelegate next,
            ICorrelationIdProvider correlationIdProvider)
        {
            this.next = next;
            _correlationIdProvider = correlationIdProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            #region logger
            var correlationId = _correlationIdProvider.Get();
            var METHOD_NAME = LOGGER_NAME + nameof(InvokeAsync);

            logger.Debug(correlationId, null, METHOD_NAME, null, $"{nameof(InvokeAsync)} method has started");
            #endregion

            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                #region logger
                logger.Error(correlationId, null, METHOD_NAME, $"Failed to call {METHOD_NAME}(): {exception.Message}", null, exception, null);
                #endregion
                await HandleExceptionAsync(context, exception);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = (int)HttpStatusCode.InternalServerError;
            var contentType = context.Request.ContentType ?? "application/json";

            string body = JsonConvert.SerializeObject(new
            {
                errorCode = ErrorCode.InternalError, // internal server error
                success = false,
                message = ExceptionLogger.GetFullMessageText(exception),
                stackTrace = exception.StackTrace
            });
            context.Response.ContentType = contentType;
            context.Response.StatusCode = code;
            return context.Response.WriteAsync(body);
        }
    }
}