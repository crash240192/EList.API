namespace EList.Models.Enums
{
    /// <summary>
    /// Оценка комментария на странице мероприятия.
    /// Values start at 1 so default(0) is never a valid vote (avoids null/0 confusion in clients).
    /// </summary>
    public enum MessageVoteValue
    {
        Like = 1,
        Dislike = 2
    }
}
