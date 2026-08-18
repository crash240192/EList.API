namespace EList.Models.Enums
{
    /// <summary>
    /// Тип модерационного ограничения. Срок задаётся отдельно (<c>endsAt</c>).
    /// </summary>
    public enum ModerationPenaltyType
    {
        /// <summary>Полная блокировка входа в аккаунт.</summary>
        SuspendAccount = 0,
        /// <summary>Организация скрыта / неактивна.</summary>
        SuspendOrganization = 1,
        /// <summary>Запрет создавать мероприятия.</summary>
        BanEventCreate = 2,
        /// <summary>Запрет участвовать в любых мероприятиях.</summary>
        BanEventParticipate = 3,
        /// <summary>Запрет писать сообщения (чаты мероприятий).</summary>
        BanMessaging = 4,
        /// <summary>Запрет быть организатором (новые назначения и создание событий).</summary>
        BanOrganize = 5,
        /// <summary>Бан на конкретном мероприятии (чёрный список).</summary>
        BanFromEvent = 6
    }
}
