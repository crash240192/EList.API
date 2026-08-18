namespace EList.Models.Notifications
{
    public enum UserNotificationType
    {
        EventCreated = 0,
        EventUpdated = 1,
        EventCancelled = 2,
        EventFinished = 3,
        /// <summary>Мероприятие восстановлено после отмены модерацией.</summary>
        EventRestored = 4,

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
        EventRatingDeleted = 62,

        /// <summary>На ваш профиль / контент поступила жалоба (без личности жалобщика).</summary>
        ContentReportFiledAgainstYou = 70,
        /// <summary>Новая жалоба в очереди организаторов события.</summary>
        ContentReportNewInOrganizerQueue = 71,
        /// <summary>Новая жалоба или эскалация в очереди площадки.</summary>
        ContentReportNewInPlatformQueue = 72,
        /// <summary>Модератор вынес предупреждение.</summary>
        ContentReportWarningIssued = 73,
        /// <summary>Контент скрыт или удалён по жалобе.</summary>
        ContentReportContentModerated = 74,
        /// <summary>Жалоба рассмотрена или отклонена (для автора жалобы).</summary>
        ContentReportReviewed = 75,
        /// <summary>Аккаунт приостановлен модерацией.</summary>
        ContentReportAccountSuspended = 76,
        /// <summary>Организация приостановлена модерацией.</summary>
        ContentReportOrganizationSuspended = 77,
        /// <summary>Снят с организаторов мероприятия.</summary>
        ContentReportOrganizatorRemoved = 78,
        /// <summary>Аватарка или обложка сброшена модерацией.</summary>
        ContentReportAvatarReset = 79,
        /// <summary>Наложен временный или постоянный модерационный штраф.</summary>
        ContentReportPenaltyIssued = 80
    }
}
