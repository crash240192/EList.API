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

        public async Task<Guid?> CreateAlbumAsync(MediaAlbumDto item)
        {
            item.CreateDate = DateTimeOffset.Now;
            item.UpdateDate = DateTimeOffset.Now;
            var result = (Guid) await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task<List<MediaAlbumDto>> GetAccountAlbumsAsync(Guid accountId)
        {
            var result = await _connection.Albums.Where(i => i.AccountId == accountId).ToListAsync();
            return result;
        }

        public async Task<MediaAlbumDto> GetAlbumAsync(Guid id)
        {
            var result = await _connection.Albums.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<List< MediaAlbumDto>> GetEventAlbumsAsync(Guid eventId)
        {
            var result = await _connection.Albums.Where(i => i.EventId == eventId).ToListAsync();
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
                AccountId= accountId,
                PhotoId= fileId,
                AssignmentDate= DateTimeOffset.Now
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
