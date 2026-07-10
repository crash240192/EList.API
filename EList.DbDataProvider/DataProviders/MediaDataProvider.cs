using EList.Common.Extensions;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using EList.Models.Accounts;
using EList.Models.Media;
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
                .LoadWith(i => i.EventRelation)
                .LoadWith(i => i.AccountRelation)
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

        public async Task<ListResponse<EventAlbumsGroupDto>> GetEventsAlbumsAsync(
            Guid accountId, Guid? curAccountId, int? pageIndex = null, int? pageSize = null)
        {
            var eventsQuery = _connection.Events
                .LoadWith(i => i.Parameters)
                .LoadWith(i => i.Organizators)
                .Where(e => e.Organizators.Any(o => o.AccountId == accountId)
                    || e.Participants.Any(p => p.AccountId == accountId))
                .OrderByDescending(i => i.StartTime)
                .AsQueryable();

            if (curAccountId != null)
                eventsQuery = eventsQuery
                .Where(e => e.Albums.Any(rel =>
                    e.Organizators.Any(o => o.AccountId == curAccountId)
                    || (
                        e.Parameters.Private == true
                        && (e.WhiteList.Any(w => w.AccountId == curAccountId) || !e.WhiteList.Any())
                        && (
                            e.Invitations.Any(inv => inv.InvitedAccountId == curAccountId)
                            || e.Participants.Any(p => p.AccountId == curAccountId)
                        )
                    )
                    || (
                        e.Parameters.Private != true
                        && !e.BlackList.Any(b => b.AccountId == curAccountId)
                        && (
                            e.Participants.Any(p => p.AccountId == curAccountId)
                            || e.Invitations.Any(inv => inv.InvitedAccountId == curAccountId)
                            || rel.Album.Parameters == null
                            || !rel.Album.Parameters.Private
                        )
                    )));
            else
                eventsQuery = eventsQuery
                    .Where(i => i.Albums.Any(a =>
                        i.Parameters.Private != null && a.Album.Parameters.Private != true));

            eventsQuery = eventsQuery.OrderBy(e => e.StartTime);

            var totalCount = await eventsQuery.CountAsync();

            var pageIdx = pageIndex ?? 0;
            var pageSz = pageSize ?? totalCount;
            if (pageSz <= 0)
                pageSz = totalCount;

            var pagedEventIds = await eventsQuery
                .Select(e => e.Id)
                .Skip(pageIdx * pageSz)
                .Take(pageSz)
                .ToListAsync();

            if (!pagedEventIds.Any())
                return new ListResponse<EventAlbumsGroupDto>(totalCount, new List<EventAlbumsGroupDto>());

            var events = await _connection.Events
                .LoadWith(e => e.Types).ThenLoad(t => t.Type).ThenLoad(ty => ty.EventCategory)
                .Where(e => pagedEventIds.Contains(e.Id))
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            var relations = new List<EventAlbumRelationDto>();

            if (curAccountId != null)
            {
                relations = await (
                from e in _connection.Events
                where pagedEventIds.Contains(e.Id)
                from rel in e.Albums
                where
                    e.Organizators.Any(o => o.AccountId == curAccountId)
                    || (
                        e.Parameters.Private == true
                        && (e.WhiteList.Any(w => w.AccountId == curAccountId) || !e.WhiteList.Any())
                        && (
                            e.Invitations.Any(inv => inv.InvitedAccountId == curAccountId)
                            || e.Participants.Any(p => p.AccountId == curAccountId)
                        )
                    )
                    || (
                        e.Parameters.Private != true
                        && !e.BlackList.Any(b => b.AccountId == curAccountId)
                        && (
                            e.Participants.Any(p => p.AccountId == curAccountId)
                            || e.Invitations.Any(inv => inv.InvitedAccountId == curAccountId)
                            || rel.Album.Parameters == null
                            || !rel.Album.Parameters.Private
                        )
                    )
                select rel
                ).ToListAsync();
            }
            else
            {
                relations = await (
                from e in _connection.Events
                where pagedEventIds.Contains(e.Id)
                from rel in e.Albums
                where
                    e.Parameters.Private != true
                select rel
                ).ToListAsync();
            }

            if (!relations.Any())
                return new ListResponse<EventAlbumsGroupDto>(totalCount, new List<EventAlbumsGroupDto>());

            var albumIds = relations.Select(r => r.AlbumId).Distinct().ToList();
            var albums = await _connection.Albums
                .LoadWith(a => a.Parameters)
                .Where(a => albumIds.Contains(a.Id))
                .ToListAsync();

            var relationByAlbumId = relations
                .GroupBy(r => r.AlbumId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var album in albums)
                album.EventRelation = relationByAlbumId[album.Id];

            var albumsByEventId = albums
                .GroupBy(a => a.EventRelation.EventId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = events.Select(e => new EventAlbumsGroupDto
            {
                Event = e,
                Albums = albumsByEventId.GetValueOrDefault(e.Id) ?? new List<MediaAlbumDto>()
            }).ToList();

            return new ListResponse<EventAlbumsGroupDto>(totalCount, result);
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

        public async Task<FileAlbumRelationDto> GetFileAsync(Guid fileId, Guid albumId)
        {
            var result = await _connection.AlbumFiles.FirstOrDefaultAsync(i => i.AlbumId == albumId && fileId == fileId);
            return result;
        }

        public async Task<bool> CheckFileExistsAsync(List<Guid> fileIds)
        {
            var result = await _connection.AlbumFiles.Where(i => fileIds.Contains(i.FileId))
                .Select(i => i.FileId)
                .Distinct()
                .CountAsync();
            if (result == fileIds.Count)
                return true;
            return false;
        }

        public async Task<List<Guid>> GetFilesNotExistsInAnotherAlbumsAsync(List<Guid> fileIds, Guid exceptAlbumId)
        {
            if (!fileIds.NullSafeAny())
                return null;

            var filesInAnotherAlbums = await _connection.AlbumFiles
                .Where(i => i.AlbumId != exceptAlbumId && fileIds.Contains(i.FileId))
                .Select(i => i.FileId)
                .ToListAsync();

            if (filesInAnotherAlbums.NullSafeAny())
                fileIds = fileIds?.Where(i => !filesInAnotherAlbums.Contains(i)).ToList();

            return fileIds;
        }

        public async Task<bool> SomeAlbumContainsThisFileAsync(Guid fileId)
        {
            var result = await _connection.AlbumFiles
                .AnyAsync(i => i.FileId == fileId);
            return result;
        }

        public async Task DeleteFilesAsync(List<Guid> fileIds)
        {
            await _connection.AlbumFiles.Where(i => fileIds.Contains(i.FileId))
                .DeleteAsync();
        }

        public async Task DeleteAlbumAsync(Guid albumId)
        {
            await _connection.AccountAlbums.Where(i => i.AlbumId == albumId)
                .DeleteAsync();

            await _connection.EventAlbums.Where(i => i.AlbumId == albumId)
                .DeleteAsync();

            await _connection.EventAlbumParameters.Where(i => i.AlbumId == albumId)
                .DeleteAsync();

            await _connection.AlbumFiles.Where(i => i.AlbumId == albumId)
                .DeleteAsync();

            await _connection.Albums.Where(i => i.Id == albumId)
                .DeleteAsync();
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

        public async Task<AccountAvatarDto> GetAvatarAsync(Guid fileId)
        {
            var result = await _connection.AccountAvatars.FirstOrDefaultAsync(i => i.PhotoId == fileId);
            return result;
        }

        public async Task DeleteAvatarAsync(Guid fileId)
        {
            await _connection.AccountAvatars.Where(i => i.PhotoId == fileId)
                .DeleteAsync();
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
