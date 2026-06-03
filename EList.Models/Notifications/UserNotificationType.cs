namespace EList.Models.Notifications
{
    public enum UserNotificationType
    {
        EventCreated = 0,
        EventUpdated = 1,
        EventCancelled = 2,
        EventFinished = 3,

        Subscribed = 10,
        Unsubsceibed = 11,
        RelatedPersonSubscribed = 12,
        RelatedPersonUnsubscribed = 13,

        Participated = 20,
        Unparticipated = 21,

        MessageReplied = 31,

        AddedToBlackList = 41,
        AddedToWhiteList = 42,
        RemovedFromBlackList = 43,
        RemovedFromWhiteList = 44,

        NewInvitation = 51,
    }
}
