using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.ContactData;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers.ContactData
{
    [Authorize]
    [ApiController]
    [Route("/api/contacts")]
    [LoggerHandlerWebApiFilter]
    public class ContactsController : ControllerBase
    {
        private readonly IContactsService _contactDataService;
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IAccountDataHolder _accountDataHolder;
        public ContactsController(IContactsService contactDataService,
            IDataConnectionProvider connectionProvider,
            IAccountDataHolder accountDataHolder)
        {
            _contactDataService = contactDataService;
            _connectionProvider = connectionProvider;
            _accountDataHolder = accountDataHolder;
        }

        /// <summary>
        /// Создание записи типа контактных данных
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("contactTypes/create")]
        public async Task<CommandResult<Guid?>> CreateContactTypeAsync(ContactTypeRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _contactDataService.CreateContactTypeAsync(request);
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
        /// Получение записи типа контактных данных по id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("contactTypes/get/{id}")]
        public async Task<CommandResult<ContactType?>> GetContactTypeAsync(Guid id)
        {
            var result = await _contactDataService.GetContactTypeAsync(id);
            return result;
        }

        /// <summary>
        /// Обновление записи типа контактных данных по id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("contactTypes/update/{id}")]
        public async Task<CommandResult> UpdateContactTypeAsync(Guid id, ContactTypeRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _contactDataService.UpdateContactTypeAsync(id, request);
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
        /// Возвращает список всех записей типов контактных данных
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("contactTypes/getAll")]
        public async Task<CommandResult<List<ContactType>>> GetAllContactTypesAsync()
        {
            var result = await _contactDataService.GetAllContactTypesAsync();
            return result;
        }

        /// <summary>
        /// Soft-delete типа контакта (active = false)
        /// </summary>
        [HttpDelete("contactTypes/delete/{id}")]
        public async Task<CommandResult> DeleteContactTypeAsync(Guid id)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _contactDataService.DeleteContactTypeAsync(id);
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
        /// Создание записи контактных данных
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateContactAsync(ContactRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _contactDataService.CreateContactAsync(request);
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
        /// Получение записи контактных данных по id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("get/{id}")]
        public async Task<CommandResult<ContactDataItem?>> GetContactAsync(Guid id)
        {
            var result = await _contactDataService.GetAccountContactAsync(id);
            return result;
        }

        /// <summary>
        /// Обновление записи контактных данных по id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("update/{id}")]
        public async Task<CommandResult> UpdateContactAsync(Guid id, ContactRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _contactDataService.UpdateContactAsync(id, request);
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
        /// Возвращает список всех записей контактных данных пользователя
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAccountContacts")]
        public async Task<CommandResult<List<ContactDataItem>?>> GetAccountContactsAsync()
        {
            var result = await _contactDataService.GetAccountContactsAsync(_accountDataHolder.AccountId.Value);
            return result;
        }

        /// <summary>
        /// Возвращает авторизационный контакт пользователя
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAuthorizationContact")]
        public async Task<CommandResult<ContactDataItem>> GetAuthorizationContactAsync()
        {
            var result = await _contactDataService.GetAuthorizationContactAsync();
            return result;

        }

        /// <summary>
        /// Возвращает список всех записей контактных данных указанного пользователя
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAccountContacts/{accountId}")]
        public async Task<CommandResult<List<ContactDataItem>?>> GetAccountContactsAsync(Guid accountId)
        {
            var result = await _contactDataService.GetAccountContactsAsync(accountId);
            return result;

        }

        /// <summary>
        /// Создание контактных данных организации
        /// </summary>
        [HttpPost("organization/{organizationId}/create")]
        public async Task<CommandResult<Guid?>> CreateOrganizationContactAsync(Guid organizationId, ContactRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _contactDataService.CreateOrganizationContactAsync(organizationId, request);
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
        /// Получение контакта организации по id
        /// </summary>
        [HttpGet("organization/get/{id}")]
        public async Task<CommandResult<ContactDataItem?>> GetOrganizationContactAsync(Guid id)
        {
            var result = await _contactDataService.GetOrganizationContactAsync(id);
            return result;
        }

        /// <summary>
        /// Обновление контакта организации
        /// </summary>
        [HttpPut("organization/update/{id}")]
        public async Task<CommandResult> UpdateOrganizationContactAsync(Guid id, ContactRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _contactDataService.UpdateOrganizationContactAsync(id, request);
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
        /// Список контактных данных организации
        /// </summary>
        [HttpGet("organization/{organizationId}/getAll")]
        public async Task<CommandResult<List<ContactDataItem>?>> GetOrganizationContactsAsync(Guid organizationId)
        {
            var result = await _contactDataService.GetOrganizationContactsAsync(organizationId);
            return result;
        }
    }
}
