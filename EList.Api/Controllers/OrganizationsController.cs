using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Orders;
using EList.Models.Organizations;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/organizations")]
    [LoggerHandlerWebApiFilter]
    public class OrganizationsController : ControllerBase
    {
        private readonly IOrganizationsService _organizationsService;
        private readonly IOrdersService _ordersService;
        private readonly IDataConnectionProvider _connectionProvider;

        public OrganizationsController(IOrganizationsService organizationsService,
            IOrdersService ordersService, IDataConnectionProvider connectionProvider)
        {
            _organizationsService = organizationsService;
            _ordersService = ordersService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Создать организацию
        /// </summary>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateOrganizationAsync([FromBody] OrganizationRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.CreateOrganizationAsync(request);
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
        /// Получить организацию по id
        /// </summary>
        [HttpGet("get/{organizationId}")]
        public async Task<CommandResult<OrganizationResponse?>> GetOrganizationAsync(Guid organizationId)
        {
            var result = await _organizationsService.GetOrganizationAsync(organizationId);
            return result;
        }

        /// <summary>
        /// Получить организации текущего пользователя
        /// </summary>
        [HttpGet("my")]
        public async Task<CommandResult<List<OrganizationResponse>?>> GetMyOrganizationsAsync()
        {
            var result = await _organizationsService.GetMyOrganizationsAsync();
            return result;

        }

        /// <summary>
        /// Получить организации указанного пользователя
        /// </summary>
        [HttpGet("get/byAccountId/{accountId}")]
        public async Task<CommandResult<List<OrganizationResponse>?>> GetUserOrganizationsAsync(Guid accountId)
        {
            var result = await _organizationsService.GetUserOrganizationsAsync(accountId);
            return result;

        }

        /// <summary>
        /// Обновить профиль организации
        /// </summary>
        [HttpPut("update/{organizationId}")]
        public async Task<CommandResult> UpdateOrganizationAsync(Guid organizationId, [FromBody] OrganizationRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.UpdateOrganizationAsync(organizationId, request);
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
        /// Включить / выключить организацию
        /// </summary>
        [HttpPut("setActive/{organizationId}")]
        public async Task<CommandResult> SetOrganizationActiveAsync(Guid organizationId, [FromQuery] bool active)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.SetOrganizationActiveAsync(organizationId, active);
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
        /// Получить участников организации
        /// </summary>
        [HttpGet("members/{organizationId}")]
        public async Task<CommandResult<List<OrganizationMemberResponse>?>> GetMembersAsync(Guid organizationId)
        {
            var result = await _organizationsService.GetMembersAsync(organizationId);
            return result;
        }

        /// <summary>
        /// Добавить менеджера в организацию
        /// </summary>
        [HttpPost("managers/add/{organizationId}")]
        public async Task<CommandResult<Guid?>> AddManagerAsync(Guid organizationId, [FromBody] AddOrganizationMemberRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.AddManagerAsync(organizationId, request);
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
        /// Добавить билетёра в организацию
        /// </summary>
        [HttpPost("ticket-takers/add/{organizationId}")]
        public async Task<CommandResult<Guid?>> AddTicketTakerAsync(Guid organizationId, [FromBody] AddOrganizationMemberRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.AddTicketTakerAsync(organizationId, request);
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
        /// Сменить роль участника (Manager ↔ TicketTaker). Только владелец.
        /// </summary>
        [HttpPut("members/role/{organizationId}")]
        public async Task<CommandResult> UpdateMemberRoleAsync(
            Guid organizationId,
            [FromBody] UpdateOrganizationMemberRoleRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.UpdateMemberRoleAsync(organizationId, request);
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
        /// Удалить участника из организации
        /// </summary>
        [HttpDelete("members/remove/{organizationId}/{accountId}")]
        public async Task<CommandResult> RemoveMemberAsync(Guid organizationId, Guid accountId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.RemoveMemberAsync(organizationId, accountId);
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
        /// Активировать / деактивировать участника организации
        /// </summary>
        [HttpPut("members/setActive/{organizationId}/{accountId}")]
        public async Task<CommandResult> SetMemberActiveAsync(Guid organizationId, Guid accountId, [FromQuery] bool active)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.SetMemberActiveAsync(organizationId, accountId, active);
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
        /// Передать владение организацией
        /// </summary>
        [HttpPost("transferOwnership/{organizationId}")]
        public async Task<CommandResult> TransferOwnershipAsync(Guid organizationId, [FromBody] TransferOwnershipRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.TransferOwnershipAsync(organizationId, request);
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
        /// Сохранить юридические реквизиты
        /// </summary>
        [HttpPut("legal/{organizationId}")]
        public async Task<CommandResult> UpsertLegalAsync(Guid organizationId, [FromBody] OrganizationLegalRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.UpsertLegalAsync(organizationId, request);
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
        /// Получить юридические реквизиты
        /// </summary>
        [HttpGet("legal/{organizationId}")]
        public async Task<CommandResult<OrganizationLegalResponse?>> GetLegalAsync(Guid organizationId)
        {
            var result = await _organizationsService.GetLegalAsync(organizationId);
            return result;
        }

        /// <summary>
        /// Сохранить платёжные реквизиты
        /// </summary>
        [HttpPut("payout/{organizationId}")]
        public async Task<CommandResult> UpsertPayoutAsync(Guid organizationId, [FromBody] OrganizationPayoutRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.UpsertPayoutAsync(organizationId, request);
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
        /// Получить платёжные реквизиты
        /// </summary>
        [HttpGet("payout/{organizationId}")]
        public async Task<CommandResult<OrganizationPayoutResponse?>> GetPayoutAsync(Guid organizationId)
        {
            var result = await _organizationsService.GetPayoutAsync(organizationId);
            return result;
        }

        /// <summary>
        /// Запустить онбординг организации в платёжной системе (ЮKassa / stub)
        /// </summary>
        [HttpPost("payout/{organizationId}/provider-onboarding/start")]
        public async Task<CommandResult<OrganizationProviderOnboardingResponse?>> StartProviderOnboardingAsync(
            Guid organizationId,
            [FromBody] OrganizationProviderOnboardingRequest? request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.StartProviderOnboardingAsync(organizationId, request);
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
        /// Поиск реквизитов организации/ИП по ИНН (автозаполнение формы)
        /// </summary>
        [HttpGet("lookup/inn/{inn}")]
        public async Task<CommandResult<OrganizationRegistryParty?>> LookupByInnAsync(string inn)
        {
            var result = await _organizationsService.LookupByInnAsync(inn);
            return result;
        }

        /// <summary>
        /// Отправить организацию на верификацию
        /// </summary>
        [HttpPost("verification/submit/{organizationId}")]
        public async Task<CommandResult> SubmitVerificationAsync(Guid organizationId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.SubmitVerificationAsync(organizationId);
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
        /// Включить / выключить продажу билетов
        /// </summary>
        [HttpPut("tickets/setEnabled/{organizationId}")]
        public async Task<CommandResult> SetCanSellTicketsAsync(Guid organizationId, [FromQuery] bool enabled)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationsService.SetCanSellTicketsAsync(organizationId, enabled);
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
        /// Сводка билетов по событиям организации (hub)
        /// </summary>
        [HttpGet("{organizationId}/events/ticket-summary")]
        public async Task<CommandResult<List<OrganizationEventTicketSummaryItem>>> GetEventsTicketSummaryAsync(
            Guid organizationId,
            [FromQuery] int limit = 100)
        {
            var result = await _ordersService.GetOrganizationEventsTicketSummaryAsync(organizationId, limit);
            return result;
        }
    }
}
