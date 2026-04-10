using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Media;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class MediaRepository : IMediaRepository
    {
        private readonly IMapper _mapper;
        private readonly IMediaDataProvider _mediaDataProvider;
        public MediaRepository(IMapper mapper,
            IMediaDataProvider mediaDataProvider) 
        {
            _mapper = mapper;
            _mediaDataProvider = mediaDataProvider;
        }

        public async Task<Guid?> CreateAlbumAsync(Guid accountId, CreateAlbumRequest request)
        {
            var mapedRequest = new MediaAlbumDto
            {
                AccountId = accountId,
                EventId = request.EventId,
                Description = request.Description,
                Name = request.Name
            };
            var result = await _mediaDataProvider.CreateAlbumAsync(mapedRequest);
            return result;
        }

        public async Task<List<MediaAlbum>> GetAccountAlbumsAsync(Guid accountId)
        {
            var items = await _mediaDataProvider.GetAccountAlbumsAsync(accountId);
            var result = _mapper.Map<List<MediaAlbum>>(items);
            //var result = new PagedList<MediaAlbum>(items?.Count() ?? 0, albums, 1, items?.Count() ?? 0);
            return result;
        }

        public async Task<MediaAlbum> GetAlbumAsync(Guid id)
        {
            var album = await _mediaDataProvider.GetAlbumAsync(id);
            var result = _mapper.Map<MediaAlbum>(album);
            return result;
        }

        public async Task<List<MediaAlbum>> GetEventAlbumsAsync(Guid eventId)
        {
            var items = await _mediaDataProvider.GetEventAlbumsAsync(eventId);
            var result = _mapper.Map<List<MediaAlbum>>(items);
            //var result = new PagedList<MediaAlbum>(items?.Count() ?? 0, albums, 1, items?.Count() ?? 0);
            return result;
        }

        #region account avatars
        public async Task SetNewAccountAvatarAsync(Guid accountId, Guid fileId)
        {
            await _mediaDataProvider.SetNewAccountAvatarAsync(accountId, fileId);
        }

        public async Task<List<Guid>?> GetAccountAvatarsAsync(Guid accountId)
        {
            var fileIds = await _mediaDataProvider.GetAccountAvatarsAsync(accountId);
            return fileIds;
        }

        public async Task<Guid?> GetLastAccountAvatarAsync(Guid accountId)
        {
            var result = await _mediaDataProvider.GetLastAccountAvatarAsync(accountId);
            return result;
        }
        #endregion

        #region organizationAvatars
        public async Task SetNewOrganizationAvatarAsync(Guid organizationId, Guid fileId)
        {
            await _mediaDataProvider.SetNewOrganizationAvatarAsync(organizationId, fileId);
        }

        public async Task<List<Guid>?> GetOrganizationAvatarsAsync(Guid organizationId)
        {
            var fileIds = await _mediaDataProvider.GetOrganizationAvatarsAsync(organizationId);
            return fileIds;
        }

        public async Task<Guid?> GetLastOrganizationAvatarAsync(Guid organizationId)
        {
            var result = await _mediaDataProvider.GetLastOrganizationAvatarAsync(organizationId);
            return result;
        }
        #endregion
    }
}
