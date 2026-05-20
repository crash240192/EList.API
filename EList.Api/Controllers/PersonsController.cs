using EList.Api.Extensions;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Person;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Diagnostics;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/persons")]
    [LoggerHandlerWebApiFilter]
    public class PersonsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.PersonController.";
        #endregion

        private readonly IPersonsService _personService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public PersonsController(ICorrelationIdProvider correlationIdProvider, 
            IPersonsService personService,
            IDataConnectionProvider connectionProvider)
        {
            _personService = personService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Создание записи личных данных
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("set")]
        public async Task<CommandResult<Guid?>> CreatePersonInfoAsync(PersonRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreatePersonInfoAsync)}";
            
            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _personService.CreatePersonInfoAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Получение личных данных
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("get/{accountId}")]
        public async Task<CommandResult<PersonInfo?>> GetPersonInfoByAccountIdAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetPersonInfoByAccountIdAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _personService.GetPersonInfoByAccountIdAsync(accountId);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }


        /// <summary>
        /// Получение личных данных
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("get")]
        public async Task<CommandResult<PersonInfo?>> GetPersonInfoAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetPersonInfoAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _personService.GetPersonInfoByTokenAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
        ///// <summary>
        ///// Обновление личных данных
        ///// </summary>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //[HttpPut("update")]
        //public async Task<CommandResult> UpdatePersonAsync(PersonRequest request)
        //{
        //    var correlationId = _correlationIdProvider.Get();
        //    var execTime = Stopwatch.StartNew();
        //    var methodName = $"{LOGGER_NAME}{nameof(UpdatePersonAsync)}";

        //    try
        //    {
        //        await _connectionProvider.StartNewTransactionAsync();
        //        logger.Debug(correlationId, null, methodName, $"Method started", null);

        //        var token = this.GetToken();

        //        var result = await _personService.UpdatePersonInfoAsync(token, request);
        //        if (!result.Success)
        //        {
        //            await _connectionProvider.RollbackTransactionAsync();
        //            return CommandResult.Fail(result.ErrorCode, result.Message);
        //        }

        //        await _connectionProvider.CommitTransactionAsync();

        //        logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
        //        throw;
        //    }
        //}
    }
}
