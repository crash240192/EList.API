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
        /// <summary>Пользователя исключили из участников мероприятия (kick / BL / WL).</summary>
        RemovedFromEvent = 22,

        MessageReplied = 31,

        AddedToBlackList = 41,
        AddedToWhiteList = 42,
        RemovedFromBlackList = 43,
        RemovedFromWhiteList = 44,
        NotInWhiteList = 45,

        NewInvitation = 51,
        /// <summary>Приглашённый принял приглашение (для организаторов / пригласившего).</summary>
        InvitationAccepted = 52,
        /// <summary>Приглашённый отклонил приглашение.</summary>
        InvitationDeclined = 53,
        /// <summary>Организатор отменил приглашение (для приглашённого).</summary>
        InvitationCancelled = 54,

        NewEventRating = 60,
        EventRatingChanged = 61,
        EventRatingDeleted = 62,

        /// <summary>Пользователя добавили в организацию (менеджер).</summary>
        OrganizationMemberAdded = 90,
        /// <summary>Пользователя удалили из организации.</summary>
        OrganizationMemberRemoved = 91,
        /// <summary>Участие в организации деактивировано.</summary>
        OrganizationMemberDeactivated = 92,
        /// <summary>Владение организацией передано.</summary>
        OrganizationOwnershipTransferred = 93,
        /// <summary>Организация прошла верификацию.</summary>
        OrganizationVerificationApproved = 94,
        /// <summary>Верификация организации отклонена.</summary>
        OrganizationVerificationRejected = 95,

        /// <summary>Аккаунт назначен организатором мероприятия.</summary>
        EventOrganizatorAssigned = 100,
        /// <summary>Аккаунт снят с организаторов мероприятия.</summary>
        EventOrganizatorRemoved = 101,

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
