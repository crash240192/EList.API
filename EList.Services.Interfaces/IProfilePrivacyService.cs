using EList.Common.Models;
using EList.Models.Accounts;
using EList.Models.Privacy;

namespace EList.Services.Interfaces
{
    public interface IProfilePrivacyService
    {
        Task<CommandResult<AccountPrivacySettings>> GetMySettingsAsync();

        Task<CommandResult<AccountPrivacySettings>> UpdateMySettingsAsync(UpdatePrivacySettingsRequest request);

        Task<CommandResult<CanInviteResult>> CanInviteAsync(Guid targetAccountId);

        Task<CommandResult<List<CanInviteResult>>> CanInviteBatchAsync(IEnumerable<Guid> targetAccountIds);

        /// <summary>
        /// Может ли <paramref name="inviterAccountId"/> отправить приглашение <paramref name="inviteeAccountId"/>.
        /// </summary>
        Task<CommandResult> AssertCanSendInvitationAsync(Guid inviterAccountId, Guid inviteeAccountId);

        Task<bool> CanViewerSeeAsync(Guid ownerAccountId, Guid? viewerAccountId, Models.Enums.PrivacyAudience audience);

        Task<AccountPrivacySettings> GetOrDefaultAsync(Guid accountId);

        /// <summary>
        /// Ограничивает поля аккаунта для просмотра чужого профиля (город, координаты, аватар).
        /// </summary>
        Task<Account> ApplyAccountViewPolicyAsync(Account account, Guid? viewerAccountId);

        Task<bool> CanViewerSeeProfilePhotosAsync(Guid ownerAccountId, Guid? viewerAccountId);
    }
}
