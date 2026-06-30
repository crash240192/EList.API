using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace EList.DbDataProvider.DataProviders
{
    public class MediaDataProvider : DataProviderBase, IMediaDataProvider
    {
        public MediaDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid?> CreateAlbumAsync(AlbumRequest request)
        {
            var album = new MediaAlbumDto
            {
                CreateDate = DateTimeOffset.Now,
                UpdateDate = DateTimeOffset.Now,
                Description = request.Description,
                Name = request.Name,
                WallpaperId = request.WallpaperId
            };
            var result = (Guid)await _connection.InsertWithIdentityAsync(album);

            if (request.Parameters != null)
            {
                request.Parameters.AlbumId = result;
                await _connection.InsertAsync(request.Parameters);
            }

            await _connection.InsertWithIdentityAsync(new AccountAlbumRelationDto
            {
                AccountId = request.AccountId.Value,
                AlbumId = result
            });

            return result;
        }

        public async Task AddFilesToAlbumAsync(Guid albumId, List<Guid> fileIds)
        {
            var files = fileIds.Select(i => new FileAlbumRelationDto
            {
                AlbumId = albumId,
                FileId = i
            });

            await _connection.BulkCopyAsync(files);
        }

        public async Task UpdateAlbumAsync(AlbumRequest request)
        {
            await _connection.Albums
                .Where(i => i.Id == request.Id)
                .Set(i => i.Name, request.Name)
                .Set(i => i.Description, request.Description)
                .Set(i => i.UpdateDate, DateTimeOffset.Now)
                .UpdateAsync();

            var album = await _connection.Albums
                .LoadWith(i => i.Parameters)
                .FirstOrDefaultAsync(i => i.Id == request.Id);

            if (request.Parameters != null)
            {
                if (album.Parameters != null)
                {
                    await _connection.EventAlbumParameters
                        .Where(i => i.AlbumId == request.Id)
                        .Set(i => i.ParticipantsReadonly, request.Parameters.ParticipantsReadonly)
                        .Set(i => i.HeadAlbum, request.Parameters.HeadAlbum)
                        .Set(i => i.Private, request.Parameters.Private)
                        .UpdateAsync();
                }
                else
                {
                    request.Parameters.AlbumId = request.Id.Value;
                    await _connection.InsertAsync(request.Parameters);
                }
            }
        }

        public async Task AssingAlbumToAccountAsync(Guid accountId, Guid albumId)
        {
            if (!await _connection.AccountAlbums.AnyAsync(i => i.AccountId == accountId && i.AlbumId == albumId))
                await _connection.InsertAsync(new AccountAlbumRelationDto
                {
                    AccountId = accountId,
                    AlbumId = albumId
                });
        }

        public async Task AssingAlbumToEventAsync(Guid eventId, Guid albumId)
        {
            if (!await _connection.EventAlbums.AnyAsync(i => i.EventId == eventId && i.AlbumId == albumId))
                await _connection.InsertAsync(new EventAlbumRelationDto
                {
                    EventId = eventId,
                    AlbumId = albumId
                });
        }

        public async Task<List<MediaAlbumDto>> GetAccountAlbumsAsync(Guid accountId)
        {
            var result = await _connection.Albums
                .LoadWith(i => i.Parameters)
                .LoadWith(i => i.AccountRelation)
                .Where(i => i.AccountRelation.AccountId == accountId).ToListAsync();
            return result;
        }

        public async Task<MediaAlbumDto> GetAlbumAsync(Guid id)
        {
            var result = await _connection.Albums
                .LoadWith(i => i.EventRelation)
                .LoadWith(i => i.Parameters)
                .FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<List<MediaAlbumDto>> GetEventAlbumsAsync(Guid eventId)
        {
            var result = await _connection.Albums
                .LoadWith(i => i.Parameters)
                .LoadWith(i => i.EventRelation)
                .Where(i => i.EventRelation.EventId == eventId)
                .ToListAsync();
            return result;
        }

        public async Task<ListResponse<FileAlbumRelationDto>> GetAlbumFilesAsync(Guid albumId, int? pageIndex = null, int? pageSize = null)
        {
            var request = _connection.AlbumFiles.Where(i => i.AlbumId == albumId);

            var count = await request.CountAsync();

            List<FileAlbumRelationDto> resultList;
            if (pageSize != null && pageIndex != null)
                resultList = await request.Skip(pageSize.Value * pageIndex.Value).Take(pageSize.Value).ToListAsync();
            else
                resultList = await request.ToListAsync();

            return new ListResponse<FileAlbumRelationDto>(count, resultList);
        }

        #region account avatars
        public async Task<List<Guid>?> GetAccountAvatarsAsync(Guid accountId)
        {
            var result = await _connection.AccountAvatars.Where(i => i.AccountId == accountId)
                .OrderByDescending(i => i.AssignmentDate)
                .ToListAsync();
            return result?.Select(i => i.PhotoId)?.ToList();
        }

        public async Task<Guid?> GetLastAccountAvatarAsync(Guid accountId)
        {
            var result = await _connection.AccountAvatars.Where(i => i.AccountId == accountId)
                .OrderByDescending(i => i.AssignmentDate)
                .FirstOrDefaultAsync();
            return result?.PhotoId ?? null;
        }

        public async Task SetNewAccountAvatarAsync(Guid accountId, Guid fileId)
        {
            await _connection.InsertWithIdentityAsync(new AccountAvatarDto
            {
                AccountId = accountId,
                PhotoId = fileId,
                AssignmentDate = DateTimeOffset.Now
            });
        }
        #endregion

        #region organization avatars
        public async Task<List<Guid>?> GetOrganizationAvatarsAsync(Guid organizationId)
        {
            var result = await _connection.OrganizationAvatars.Where(i => i.OrganizationId == organizationId)
                .OrderByDescending(i => i.AssignmentDate)
                .ToListAsync();
            return result?.Select(i => i.PhotoId)?.ToList();
        }

        public async Task<Guid?> GetLastOrganizationAvatarAsync(Guid organizationId)
        {
            var result = await _connection.OrganizationAvatars.Where(i => i.OrganizationId == organizationId)
                .OrderByDescending(i => i.AssignmentDate)
                .FirstOrDefaultAsync();
            return result?.PhotoId ?? null;
        }

        public async Task SetNewOrganizationAvatarAsync(Guid organizationId, Guid fileId)
        {
            await _connection.InsertWithIdentityAsync(new OrganizationAvatarDto
            {
                OrganizationId = organizationId,
                PhotoId = fileId,
                AssignmentDate = DateTimeOffset.Now
            });
        }
        #endregion
    }
}
