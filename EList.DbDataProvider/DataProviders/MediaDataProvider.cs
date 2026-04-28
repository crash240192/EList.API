using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

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
                WallpaperId = request.WallpaperId,
                Parameters = request.Parameters
            };
            var result = (Guid)await _connection.InsertWithIdentityAsync(album);

            await _connection.InsertWithIdentityAsync(new AccountAlbumRelationDto
            {
                AccountId = request.AccountId.Value,
                AlbumId = result
            });

            return result;
        }

        public async Task UpdateAlbumAsync(MediaAlbumDto item)
        {
            var album = _connection.Albums
                .LoadWith(i => i.Parameters)
                .Where(i => i.Id == item.Id)
                .Set(i => i.Name, item.Name)
                .Set(i => i.Description, item.Description)
                .Set(i => i.UpdateDate, DateTimeOffset.Now);

            if (item.Parameters != null)
                album.Set(i => i.Parameters.ParticipantsReadonly, item.Parameters.ParticipantsReadonly)
                .Set(i => i.Parameters.HeadAlbum, item.Parameters.HeadAlbum)
                .Set(i => i.Parameters.PrivateAlbum, item.Parameters.PrivateAlbum);

            await album.UpdateAsync();
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
            var result = await _connection.Albums.LoadWith(i => i.AccountRelation)
                .Where(i => i.AccountRelation.AccountId == accountId).ToListAsync();
            return result;
        }

        public async Task<MediaAlbumDto> GetAlbumAsync(Guid id)
        {
            var result = await _connection.Albums.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<List<MediaAlbumDto>> GetEventAlbumsAsync(Guid eventId)
        {
            var result = await _connection.Albums.LoadWith(i => i.EventRelation)
                .Where(i => i.EventRelation.EventId == eventId)
                .ToListAsync();
            return result;
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
