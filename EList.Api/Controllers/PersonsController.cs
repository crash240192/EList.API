using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Person;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/persons")]
    [LoggerHandlerWebApiFilter]
    public class PersonsController : ControllerBase
    {
        private readonly IPersonsService _personService;
        private readonly IDataConnectionProvider _connectionProvider;

        public PersonsController(IPersonsService personService,
            IDataConnectionProvider connectionProvider)
        {
            _personService = personService;
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
            
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _personService.CreatePersonInfoAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            var result = await _personService.GetPersonInfoByAccountIdAsync(accountId);
            return result;
        }

        /// <summary>
        /// Получение личных данных
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("get")]
        public async Task<CommandResult<PersonInfo?>> GetPersonInfoAsync()
        {
            var result = await _personService.GetPersonInfoAsync();
            return result;

        }
        ///// <summary>
        ///// Обновление личных данных
        ///// </summary>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //[HttpPut("update")]
        //public async Task<CommandResult> UpdatePersonAsync(PersonRequest request)
        //{
        //        //        //
        //    try
        //    {
        //        await _connectionProvider.StartNewTransactionAsync();
        //
        //        var token = this.GetToken();

        //        var result = await _personService.UpdatePersonInfoAsync(token, request);
        //        if (!result.Success)
        //        {
        //            await _connectionProvider.RollbackTransactionAsync();
        //            return CommandResult.Fail(result.ErrorCode, result.Message);
        //        }

        //        await _connectionProvider.CommitTransactionAsync();

        //
        //        return result;
        //    }
        //    catch
        //    {
        //        //        throw;
        //    }
        //}
    }
}
