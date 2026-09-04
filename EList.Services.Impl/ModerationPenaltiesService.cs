using EList.Common.Models;
using EList.Common.Support;
using EList.Models.ContentReports;
using EList.Models.Enums;
using EList.Models.Notifications;
using EList.Models.Participation;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;

namespace EList.Services.Impl
{
    public class ModerationPenaltiesService : IModerationPenaltiesService
    {
        private readonly IModerationPenaltiesRepository _penaltiesRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IParticipantsBWListRepository _blackListRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly INotificationsService _notificationsService;
        private readonly IAccountDataHolder _accountDataHolder;

        public ModerationPenaltiesService(
            IModerationPenaltiesRepository penaltiesRepository,
            IAccountsRepository accountsRepository,
            IOrganizationsRepository organizationsRepository,
            IParticipantsBWListRepository blackListRepository,
            IParticipationsRepository participationsRepository,
            IInvitationsRepository invitationsRepository,
            INotificationsService notificationsService,
            IAccountDataHolder accountDataHolder)
        {
            _penaltiesRepository = penaltiesRepository;
            _accountsRepository = accountsRepository;
            _organizationsRepository = organizationsRepository;
            _blackListRepository = blackListRepository;
            _participationsRepository = participationsRepository;
            _invitationsRepository = invitationsRepository;
            _notificationsService = notificationsService;
            _accountDataHolder = accountDataHolder;
        }

        public async Task LiftExpiredForAccountAsync(Guid accountId)
        {
            var expired = await _penaltiesRepository.GetExpiredUnliftedAsync(accountId: accountId);
            foreach (var penalty in expired)
                await LiftSideEffectsAndMarkAsync(penalty);
        }

        public async Task LiftExpiredForOrganizationAsync(Guid organizationId)
        {
            var expired = await _penaltiesRepository.GetExpiredUnliftedAsync(organizationId: organizationId);
            foreach (var penalty in expired)
                await LiftSideEffectsAndMarkAsync(penalty);
        }

        public async Task<CommandResult> AssertNotRestrictedAsync(
            Guid accountId,
            ModerationPenaltyType type,
            Guid? eventId = null)
        {
            await LiftExpiredForAccountAsync(accountId);

            var penalty = await _penaltiesRepository.FindActiveAsync(type, accountId, eventId: eventId);
            if (penalty == null && type == ModerationPenaltyType.BanFromEvent && eventId != null)
            {
                // Глобальный запрет участия тоже блокирует конкретное событие.
                penalty = await _penaltiesRepository.FindActiveAsync(
                    ModerationPenaltyType.BanEventParticipate, accountId);
            }

            if (penalty == null)
                return CommandResult.OK;

            return CommandResult.Fail(ErrorCode.ModerationPenaltyActive, FormatRestrictionMessage(penalty));
        }

        public async Task<List<ModerationPenalty>> GetActiveForAccountAsync(Guid accountId)
        {
            await LiftExpiredForAccountAsync(accountId);
            return await _penaltiesRepository.GetActiveByAccountAsync(accountId);
        }

        public async Task<List<ModerationPenalty>> GetActiveForOrganizationAsync(Guid organizationId)
        {
            await LiftExpiredForOrganizationAsync(organizationId);
            return await _penaltiesRepository.GetActiveByOrganizationAsync(organizationId);
        }

        public async Task<List<ModerationPenalty>> GetActiveForEventAsync(Guid eventId)
        {
            return await _penaltiesRepository.GetActiveByEventAsync(eventId);
        }

        public async Task<CommandResult<Guid>> ApplyAsync(ModerationPenalty penalty)
        {
            if (penalty.AccountId == null && penalty.OrganizationId == null)
                return CommandResult<Guid>.Fail(ErrorCode.InvalidValue, "Не указан адресат ограничения");

            if (penalty.PenaltyType == ModerationPenaltyType.BanFromEvent && penalty.EventId == null)
                return CommandResult<Guid>.Fail(ErrorCode.InvalidValue, "Для бана на мероприятии нужно указать событие");

            if (penalty.PenaltyType == ModerationPenaltyType.SuspendOrganization && penalty.OrganizationId == null)
                return CommandResult<Guid>.Fail(ErrorCode.InvalidValue, "Не указана организация");

            if (penalty.CreatedBy == Guid.Empty && _accountDataHolder.AccountId != null)
                penalty.CreatedBy = _accountDataHolder.AccountId.Value;

            if (penalty.StartsAt == default)
                penalty.StartsAt = DateTimeOffset.UtcNow;
            if (penalty.CreatedAt == default)
                penalty.CreatedAt = DateTimeOffset.UtcNow;

            var id = await _penaltiesRepository.CreateAsync(penalty);
            penalty.Id = id;

            await ImposeSideEffectsAsync(penalty);
            await _notificationsService.NotifyContentReportPenaltyIssuedAsync(penalty);

            return new CommandResult<Guid>(id);
        }

        public async Task<CommandResult> RevokeAsync(Guid penaltyId, string? comment)
        {
            if (_accountDataHolder.AccountId == null || !_accountDataHolder.IsPlatformModeratorOrAbove)
                return CommandResult.Fail(ErrorCode.AccessError, "Снять ограничение может только модератор площадки");

            var penalty = await _penaltiesRepository.GetByIdAsync(penaltyId);
            if (penalty == null)
                return CommandResult.Fail(ErrorCode.ModerationPenaltyNotFound, "Ограничение не найдено");

            if (!penalty.IsActive)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Ограничение уже не действует");

            await _penaltiesRepository.MarkRevokedAsync(penaltyId, _accountDataHolder.AccountId.Value, DateTimeOffset.UtcNow);
            penalty.RevokedAt = DateTimeOffset.UtcNow;
            penalty.LiftedAt = DateTimeOffset.UtcNow;
            await ApplyLiftSideEffectsAsync(penalty);
            return CommandResult.OK;
        }

        private async Task ImposeSideEffectsAsync(ModerationPenalty penalty)
        {
            switch (penalty.PenaltyType)
            {
                case ModerationPenaltyType.SuspendAccount:
                    if (penalty.AccountId != null)
                        await _accountsRepository.SetAccountActiveAsync(penalty.AccountId.Value, false);
                    break;

                case ModerationPenaltyType.SuspendOrganization:
                    if (penalty.OrganizationId != null)
                        await _organizationsRepository.SetOrganizationActiveAsync(penalty.OrganizationId.Value, false);
                    break;

                case ModerationPenaltyType.BanFromEvent:
                    if (penalty.EventId != null && penalty.AccountId != null)
                    {
                        var accountIds = new List<Guid> { penalty.AccountId.Value };
                        var participants = await _participationsRepository.GetEventParticipantIdsAsync(penalty.EventId.Value)
                            ?? new List<Guid>();
                        var wasParticipant = participants.Contains(penalty.AccountId.Value);
                        var wasInvited = await _invitationsRepository.IsUserInvitatedAsync(
                            penalty.AccountId.Value, penalty.EventId.Value);

                        await _blackListRepository.AddToBlackListAsync(new AddUsersToBWListRequest
                        {
                            EventId = penalty.EventId.Value,
                            AccountIds = accountIds
                        });
                        await _invitationsRepository.DeleteInvitationAsync(penalty.EventId.Value, accountIds);
                        await _participationsRepository.DropParticipationsAsync(penalty.EventId.Value, accountIds);

                        if (wasParticipant || wasInvited)
                            await _notificationsService.NotifyAddedToBlackListAsync(penalty.EventId.Value, accountIds);
                        if (wasParticipant)
                            await _notificationsService.NotifyRemovedFromEventAsync(penalty.EventId.Value, accountIds);
                    }
                    break;
            }
        }

        private async Task LiftSideEffectsAndMarkAsync(ModerationPenalty penalty)
        {
            await ApplyLiftSideEffectsAsync(penalty);
            await _penaltiesRepository.MarkLiftedAsync(penalty.Id, DateTimeOffset.UtcNow);
        }

        private async Task ApplyLiftSideEffectsAsync(ModerationPenalty penalty)
        {
            switch (penalty.PenaltyType)
            {
                case ModerationPenaltyType.SuspendAccount:
                    if (penalty.AccountId != null)
                    {
                        var stillActive = await _penaltiesRepository.FindActiveAsync(
                            ModerationPenaltyType.SuspendAccount, penalty.AccountId);
                        if (stillActive == null || stillActive.Id == penalty.Id)
                            await _accountsRepository.SetAccountActiveAsync(penalty.AccountId.Value, true);
                    }
                    break;

                case ModerationPenaltyType.SuspendOrganization:
                    if (penalty.OrganizationId != null)
                    {
                        var stillActive = await _penaltiesRepository.FindActiveAsync(
                            ModerationPenaltyType.SuspendOrganization, organizationId: penalty.OrganizationId);
                        if (stillActive == null || stillActive.Id == penalty.Id)
                            await _organizationsRepository.SetOrganizationActiveAsync(penalty.OrganizationId.Value, true);
                    }
                    break;

                case ModerationPenaltyType.BanFromEvent:
                    if (penalty.EventId != null && penalty.AccountId != null)
                    {
                        var stillActive = await _penaltiesRepository.FindActiveAsync(
                            ModerationPenaltyType.BanFromEvent,
                            penalty.AccountId,
                            eventId: penalty.EventId);
                        if (stillActive == null || stillActive.Id == penalty.Id)
                            await _blackListRepository.DeleteFromBlackListAsync(penalty.EventId.Value, penalty.AccountId.Value);
                    }
                    break;
            }
        }

        public static string FormatRestrictionMessage(ModerationPenalty penalty)
        {
            var until = penalty.EndsAt == null
                ? "без ограничения срока"
                : $"до {penalty.EndsAt.Value.ToLocalTime():dd.MM.yyyy HH:mm}";

            return penalty.PenaltyType switch
            {
                ModerationPenaltyType.SuspendAccount => $"Аккаунт заблокирован модерацией ({until})",
                ModerationPenaltyType.SuspendOrganization => $"Организация приостановлена модерацией ({until})",
                ModerationPenaltyType.BanEventCreate => $"Вам запрещено создавать мероприятия ({until})",
                ModerationPenaltyType.BanEventParticipate => $"Вам запрещено участвовать в мероприятиях ({until})",
                ModerationPenaltyType.BanMessaging => $"Вам запрещено писать комментарии ({until})",
                ModerationPenaltyType.BanOrganize => $"Вам запрещено быть организатором мероприятий ({until})",
                ModerationPenaltyType.BanFromEvent => $"Вам запрещено участвовать в этом мероприятии ({until})",
                _ => $"Действует модерационное ограничение ({until})"
            };
        }
    }
}
