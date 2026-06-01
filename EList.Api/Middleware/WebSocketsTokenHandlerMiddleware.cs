using EList.Common.CorrelationId;
using EList.Common.Logger;
using NLog;
using ILogger = NLog.ILogger;

namespace EList.Api.Middleware
{
    public class WebSocketsTokenHandlerMiddleware
    {

        #region NLog
        private static ILogger log = LogManager.GetCurrentClassLogger();
        private static ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.WebSocketsTokenHandlerMiddleware.";
        #endregion

        private readonly RequestDelegate next;
        private readonly ICorrelationIdProvider _correlationIdProvider;

        public WebSocketsTokenHandlerMiddleware(RequestDelegate next,
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
                if (context.Request.Path.StartsWithSegments("/ws") 
                        && context.Request.Query.TryGetValue("authorization", out var token)
                        && context.Request.Query.TryGetValue("authorization-jwt", out var jwt))
                {
                    context.Request.Headers["Authorization"] = token.ToString();
                    context.Request.Headers["Authorization-jwt"] = jwt.ToString();
                }
                
                await next(context);
            }
            catch (Exception exception)
            {
                #region logger
                logger.Error(correlationId, null, METHOD_NAME, $"Failed to call {METHOD_NAME}(): {exception.Message}", null, exception, null);
                #endregion
                throw;
            }
        }
    }
}