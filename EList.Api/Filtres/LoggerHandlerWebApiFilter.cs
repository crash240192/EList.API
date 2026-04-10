using EList.Common.Logger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using NLog;
using LogLevel = EList.Common.Logger.Enums.LogLevel;

namespace TM.Schedule.API.Attributes
{
    public class LoggerHandlerWebApiFilter : ActionFilterAttribute
    {
        private static readonly NLog.ILogger Log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper Logger = new NLogLoggerWrapper(Log);

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            try
            {
                var token = context.HttpContext.Request.Headers["Authorization"].ToString();

                var correlationId = context.HttpContext.Items[ContextKeys.CORRELATION_ID]?.ToString();
                var startDate = context.HttpContext.Items[ContextKeys.START_DATE] as DateTime?;
                var request = context.HttpContext.Items[ContextKeys.REQUEST]?.ToString();
                var loggerName = context.HttpContext.Items[ContextKeys.LOGGER_NAME]?.ToString();

                var response = JsonConvert.SerializeObject((context.Result as ObjectResult)?.Value);
                var elapsed = DateTime.Now - startDate;

                if (context.Exception == null)
                {
                    Logger.Info(
                        correlationId,
                        token,
                        loggerName,
                        response,
                        null,
                        elapsed,
                        null,
                        request);
                }
                else
                {
                    var exception = context.Exception;
                    var meta = new Dictionary<string, object>
                    {
                        { "StackTrace", exception.StackTrace }
                    };


                    if (exception.InnerException != null)
                    {
                        meta.Add("innerExMessage", exception.InnerException.Message);
                        meta.Add("innerStackTrace", exception.InnerException.StackTrace);
                    }


                    Logger.Write(
                        LogLevel.Error,
                        correlationId,
                        token,
                        loggerName,
                        request,
                        exception.Message,
                        null,
                        elapsed, exception, meta);
                }
            }
            finally
            {
                if (context.HttpContext.Items.ContainsKey(ContextKeys.REQUEST))
                    context.HttpContext.Items.Remove(ContextKeys.REQUEST);

                if (context.HttpContext.Items.ContainsKey(ContextKeys.START_DATE))
                    context.HttpContext.Items.Remove(ContextKeys.START_DATE);

                if (context.HttpContext.Items.ContainsKey(ContextKeys.LOGGER_NAME))
                    context.HttpContext.Items.Remove(ContextKeys.LOGGER_NAME);
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                var request = JsonConvert.SerializeObject(context.ActionArguments);

                context.HttpContext.Items.Add(ContextKeys.START_DATE, DateTime.Now);
                context.HttpContext.Items.Add(ContextKeys.REQUEST, request);
                context.HttpContext.Items.Add(ContextKeys.LOGGER_NAME, context.HttpContext.Request.Path);
            }
            // ReSharper disable once RedundantEmptyFinallyBlock
            finally
            {
                //ignored
            }
        }
    }
}