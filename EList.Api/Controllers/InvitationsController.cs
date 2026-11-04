using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Invitations;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/invitations")]
    [LoggerHandlerWebApiFilter]
    public class InvitationsController : Controller
    {
        private readonly IInvitationsService _invitationsService;
        private readonly IDataConnectionProvider _connectionProvider;

        public InvitationsController(IInvitationsService invitationsService, IDataConnectionProvider connectionProvider)
        {
            _invitationsService = invitationsService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Пригласить пользователей
        /// </summary>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<CommandResult> CreateInvitationsAsync(CreateInvitationsRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                var result = await _invitationsService.CreateAsync(request);
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
        /// Пригласить одного пользователя на несколько мероприятий
        /// </summary>
        [HttpPost("createToAccount")]
        public async Task<CommandResult<CreateInvitationsToAccountResult>> CreateInvitationsToAccountAsync(
            CreateInvitationsToAccountRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _invitationsService.CreateToAccountAsync(request);
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
        /// Можно ли пригласить список аккаунтов на одно мероприятие (eligibility preview).
        /// </summary>
        [HttpPost("canInviteToEvent")]
        public async Task<CommandResult<List<InviteToEventEligibility>>> CanInviteToEventByEventAsync(
            CanInviteToEventByEventRequest request)
        {
            var result = await _invitationsService.CanInviteToEventByEventAsync(request);
            return result;
        }

        /// <summary>
        /// Можно ли пригласить один аккаунт на список мероприятий (eligibility preview).
        /// </summary>
        [HttpPost("canInviteToEvents")]
        public async Task<CommandResult<List<InviteToEventEligibility>>> CanInviteToEventByAccountAsync(
            CanInviteToEventByAccountRequest request)
        {
            var result = await _invitationsService.CanInviteToEventByAccountAsync(request);
            return result;

        }

        /// <summary>
        /// Список приглашений по токену
        /// </summary>
        /// <returns></returns>
        [HttpGet("userInvitations")]
        public async Task<CommandResult<PagedList<Invitation>>> GetUserInvitationsAsync(int pageIndex = 0, int pageSize = 20)
        {
            var result = await _invitationsService.GetUserInvitationsAsync(pageIndex, pageSize);
            return result;

        }

        /// <summary>
        /// Принять приглашение
        /// </summary>
        /// <returns></returns>
        [HttpGet("accept")]
        public async Task<CommandResult> AcceptAsync(Guid invitationId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _invitationsService.AcceptAsync(invitationId);
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
        /// Отклонить приглашение
        /// </summary>
        /// <returns></returns>
        [HttpGet("decline")]
        public async Task<CommandResult> DeclineAsync(Guid invitationId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _invitationsService.DeclineAsync(invitationId);
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
        /// Отменить высланное приглашение
        /// </summary>
        /// <returns></returns>
        [HttpGet("cancel")]
        public async Task<CommandResult> CancelAsync(Guid invitationId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _invitationsService.CancelInvitationAsync(invitationId);
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
        /// Поиск приглашений
        /// </summary>
        /// <returns></returns>
        [HttpPost("search")]
        public async Task<CommandResult> SearchAsync(InvitationsSearchRequest request)
        {
            var result = await _invitationsService.SearchAsync(request);
            return result;
        }

        /// <summary>
        /// Получить количество непросмотренных приглашений
        /// </summary>
        /// <returns></returns>
        [HttpGet("notViewedCount")]
        public async Task<CommandResult<int>> GetNotViewedInvitationsCountAsync()
        {
            var result = await _invitationsService.GetNotViewedInvitationsCountAsync();
            return result;

        }

        /// <summary>
        /// Пометить как просмотренное
        /// </summary>
        /// <param name="invitationId"></param>
        /// <returns></returns>
        [HttpGet("markViewed/{invitationId}")]
        public async Task<CommandResult> ViewInvitationAsync(Guid invitationId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _invitationsService.ViewInvitationAsync(invitationId);

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
        /// Пометить все как просмотренное
        /// </summary>
        /// <returns></returns>
        [HttpGet("markViewed/all")]
        public async Task<CommandResult> ViewAllInvitationsAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _invitationsService.ViewAllInvitationsAsync();

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
    }
}
