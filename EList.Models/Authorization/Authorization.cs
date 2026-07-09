namespace EList.Models.Authorization
{
    /// <summary>
    /// Объект авторизации
    /// </summary>
    public class Authorization
    {
        /// <summary>
        /// Авторизационный токен
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// Вкл/выкл
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Ссылка на аккаунт пользователя
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Хэш информации о клиенте
        /// </summary>
        public string ClientHash { get; set; }

        /// <summary>
        /// Количество оставшихся попыток ввести текущий код активации
        /// </summary>
        public int ActivationAttemptsRemaining { get; set; }

        /// <summary>
        /// Ключ активации токена
        /// </summary>
        public string ActivationKey { get; set; }
    
        /// <summary>
        /// Дата создания записи
        /// </summary>
        public DateTimeOffset CreationDate { get; set; }

        /// <summary>
        /// Дата последней авторизации
        /// </summary>
        public DateTimeOffset AuthorizationDate { get; set; }
    }
}
