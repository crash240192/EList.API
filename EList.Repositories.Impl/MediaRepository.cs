using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.Media;
using EList.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

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

        public async Task<Guid?> CreateAlbumAsync(EventAlbumRequest request)
        {
            var mappedRequest = _mapper.Map<AlbumRequest>(request);
            var result = await _mediaDataProvider.CreateAlbumAsync(mappedRequest);
            return result;
        }

        public async Task UpdateAlbumAsync(EventAlbumRequest request)
        {
            var mappedRequest = _mapper.Map<AlbumRequest>(request);
            await _mediaDataProvider.UpdateAlbumAsync(mappedRequest);
        }

        public async Task AssingAlbumToAccountAsync(Guid accountId, Guid albumId)
        { 
            await _mediaDataProvider.AssingAlbumToAccountAsync(accountId, albumId);
        }

        public async Task AssingAlbumToEventAsync(Guid eventId, Guid albumId)
        {
            await _mediaDataProvider.AssingAlbumToEventAsync(eventId, albumId);
        }

        public async Task AddFilesToAlbumAsync(Guid albumId, List<Guid> fileIds)
        {
            await _mediaDataProvider.AddFilesToAlbumAsync(albumId, fileIds);
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

        public async Task<PagedList<AlbumFile>> GetAlbumFilesAsync(Guid albumId, int? pageIndex = null, int? pageSize = null)
        {
            var files = await _mediaDataProvider.GetAlbumFilesAsync(albumId, pageIndex, pageSize);
            var result = _mapper.Map<List<AlbumFile>>(files.Items);
            return new PagedList<AlbumFile>(files.TotalCount, result, pageIndex ?? 1, pageSize ?? files.TotalCount);
        }

        public async Task<AlbumFile> GetFileAsync(Guid fileId, Guid albumId)
        {
            var file = await _mediaDataProvider.GetFileAsync(fileId, albumId);
            var result = _mapper.Map<AlbumFile>(file);
            return result;
        }

        public async Task<List<Guid>> GetFilesNotExistsInAnotherAlbumsAsync(List<Guid> fileIds, Guid exceptAlbumId)
        {
            var result = await _mediaDataProvider.GetFilesNotExistsInAnotherAlbumsAsync(fileIds, exceptAlbumId);
            return result;
        }

        public async Task DeleteFilesAsync(List<Guid> fileIds)
        {
            await _mediaDataProvider.DeleteFilesAsync(fileIds);
        }

        public async Task DeleteAlbumAsync(Guid albumId)
        {
            await _mediaDataProvider.DeleteAlbumAsync(albumId);
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
