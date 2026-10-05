namespace EList.Models.Enums
{
    /// <summary>
    /// Аудитория видимости / кто может выполнять действие относительно владельца настроек.
    /// </summary>
    public enum PrivacyAudience
    {
        /// <summary>Все авторизованные пользователи.</summary>
        Everyone = 0,

        /// <summary>Только те, на кого подписан владелец (его подписки).</summary>
        Subscriptions = 1,

        /// <summary>Только подписчики владельца.</summary>
        Subscribers = 2,

        /// <summary>Только взаимная подписка.</summary>
        Mutual = 3,

        /// <summary>Никто (только владелец).</summary>
        Nobody = 4
    }
}
