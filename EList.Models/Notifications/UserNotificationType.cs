namespace EList.Models.Notifications
{
    public enum UserNotificationType
    {
        EventCreated = 0,
        EventUpdated = 1,
        EventCancelled = 2,
        EventFinished = 3,

        NewSubscription = 10,
        Unsubscribed = 11,
        RelatedPersonSubscribed = 12,
        RelatedPersonUnsubscribed = 13,

        Participated = 20,
        EventLeft = 21,

        MessageReplied = 31,

        AddedToBlackList = 41,
        AddedToWhiteList = 42,
        RemovedFromBlackList = 43,
        RemovedFromWhiteList = 44,
        NotInWhiteList = 45,

        NewInvitation = 51,

        NewEventRating = 60,
        EventRatingChanged = 61,
        EventRatingDeleted = 62
    }
}
