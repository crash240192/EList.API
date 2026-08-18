using LinqToDB.Mapping;
using NetTopologySuite.Geometries;

namespace EList.DbDataProvider.Models
{
    [Table("events")]
    public class EventDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("start_time")]
        public DateTimeOffset StartTime { get; set; }

        [Column("end_time")]
        public DateTimeOffset EndTime { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [Column("address")]
        public string Address { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("cancelled_at")]
        public DateTimeOffset? CancelledAt { get; set; }

        [Column("cancelled_by_account_id")]
        public Guid? CancelledByAccountId { get; set; }

        [Column("cancel_source")]
        public string? CancelSource { get; set; }

        [Column("cancel_report_id")]
        public Guid? CancelReportId { get; set; }

        [Column("event_parameters_id")]
        public Guid? EventParametersId { get; set; }

        [Column("cover_image_id")]
        public Guid? CoverImageId { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }


        [Association(ThisKey = nameof(EventParametersId), OtherKey = nameof(EventParametersDto.Id))]
        public EventParametersDto Parameters { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(EventsRatingDto.EventId))]
        public EventsRatingDto Rating { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(EventOrganizatorDto.EventId))]
        public List<EventOrganizatorDto> Organizators { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(ParticipationDto.EventId))]
        public List<ParticipationDto> Participants { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(EventTypeRelationDto.EventId))]
        public List<EventTypeRelationDto> Types { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(InvitationDto.EventId))]
        public List<InvitationDto> Invitations { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(EventAlbumRelationDto.EventId))]
        public List<EventAlbumRelationDto> Albums { get; set; }


        [Association(ThisKey = nameof(Id), OtherKey = nameof(ParticipantsBlackListItemDto.EventId))]
        public List<ParticipantsBlackListItemDto> BlackList { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(ParticipantsWhiteListItemDto.EventId))]
        public List<ParticipantsWhiteListItemDto> WhiteList { get; set; }
    }
}
