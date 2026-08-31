using EList.Common.Models;
using EList.Models.Invitations;

namespace EList.Validators.Interfaces
{
    public interface IInvitationDataValidator
    {
        CommandResult ValidateCreateRequest(CreateInvitationsRequest? request);
    }
}
