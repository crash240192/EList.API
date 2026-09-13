using EList.Models.Enums;

namespace EList.Models.Conversations
{
    /// <summary>
    /// Текущее состояние лайков/дизлайков комментария.
    /// </summary>
    public class MessageVoteResult
    {
        public Guid MessageId { get; set; }

        public int LikesCount { get; set; }

        public int DislikesCount { get; set; }

        /// <summary>
        /// Голос текущего пользователя, если он уже голосовал.
        /// </summary>
        public MessageVoteValue? CurrentUserVote { get; set; }
    }
}
