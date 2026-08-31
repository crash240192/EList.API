using EList.Common.Models;
using EList.Models.Media;

namespace EList.Validators.Interfaces
{
    public interface IMediaAlbumValidator
    {
        CommandResult ValidateAlbumRequest(EventAlbumRequest? request, bool requireName = true);

        CommandResult ValidateAddFilesRequest(AddFilesRequest? request);
    }
}
