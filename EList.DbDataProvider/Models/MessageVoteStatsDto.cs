using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Models
{
    public class MessageVoteStatsDto
    {
        public Guid MessageId { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
        public MessageVoteValue? CurrentUserVote { get; set; }
    }
}
