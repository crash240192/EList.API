using EList.Common.Logger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using LogLevel = EList.Common.Logger.Enums.LogLevel;

namespace TM.Schedule.API.Attributes
{
    public class LoggerHandlerWebApiFilter : ActionFilterAttribute
    {
        private static readonly NLog.ILogger Log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper Logger = new NLogLoggerWrapper(Log);

        private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "passwordConfirmation",
            "newPassword",
            "oldPassword",
            "confirmPassword",
            "authorizationContactValue",
            "value", // contact values (phone/email)
            "login",
            "firstName",
            "lastName",
            "patronymic",
            "birthDate",
            "inn",
            "ogrn",
            "kpp",
            "legalAddress",
            "headName",
            "bankAccount",
            "bik",
            "bankName",
            "token",
            "authorization",
            "jwt",
            "apiKey",
            "secretKey",
            "pass"
        };

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            try
            {
                // Не логируем сырой Authorization token — только факт наличия.
                var hasToken = !string.IsNullOrWhiteSpace(context.HttpContext.Request.Headers["Authorization"]);
                var tokenMarker = hasToken ? "[present]" : null;

                var correlationId = context.HttpContext.Items[ContextKeys.CORRELATION_ID]?.ToString();
                var startDate = context.HttpContext.Items[ContextKeys.START_DATE] as DateTime?;
                var request = context.HttpContext.Items[ContextKeys.REQUEST]?.ToString();
                var loggerName = context.HttpContext.Items[ContextKeys.LOGGER_NAME]?.ToString();

                var response = RedactJson(JsonConvert.SerializeObject((context.Result as ObjectResult)?.Value));
                var elapsed = DateTime.Now - startDate;

                if (context.Exception == null)
                {
                    Logger.Info(
                        correlationId,
                        tokenMarker,
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
                        tokenMarker,
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
                var request = RedactJson(JsonConvert.SerializeObject(context.ActionArguments));

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

        private static string RedactJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json ?? string.Empty;

            try
            {
                var token = JToken.Parse(json);
                RedactToken(token);
                return token.ToString(Formatting.None);
            }
            catch
            {
                // Если тело не JSON — не пишем сырьё (могли попасть секреты).
                return "[unredactable]";
            }
        }

        private static void RedactToken(JToken token)
        {
            if (token is JObject obj)
            {
                foreach (var property in obj.Properties().ToList())
                {
                    if (SensitivePropertyNames.Contains(property.Name)
                        && property.Value.Type != JTokenType.Null
                        && property.Value.Type != JTokenType.Object
                        && property.Value.Type != JTokenType.Array)
                    {
                        property.Value = "[REDACTED]";
                    }
                    else
                    {
                        RedactToken(property.Value);
                    }
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                    RedactToken(item);
            }
        }
    }
}
