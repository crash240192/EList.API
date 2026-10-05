using EList.Common.Models;
using EList.Models.Privacy;

namespace EList.Services.Interfaces
{
    public interface IProfilePrivacyService
    {
        Task<CommandResult<AccountPrivacySettings>> GetMySettingsAsync();

        Task<CommandResult<AccountPrivacySettings>> UpdateMySettingsAsync(UpdatePrivacySettingsRequest request);

        Task<CommandResult<CanInviteResult>> CanInviteAsync(Guid targetAccountId);

        /// <summary>
        /// Может ли <paramref name="inviterAccountId"/> отправить приглашение <paramref name="inviteeAccountId"/>.
        /// </summary>
        Task<CommandResult> AssertCanSendInvitationAsync(Guid inviterAccountId, Guid inviteeAccountId);

        Task<bool> CanViewerSeeAsync(Guid ownerAccountId, Guid? viewerAccountId, Models.Enums.PrivacyAudience audience);

        Task<AccountPrivacySettings> GetOrDefaultAsync(Guid accountId);
    }
}
