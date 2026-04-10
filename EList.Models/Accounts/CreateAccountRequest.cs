namespace EList.Models.Accounts
{
    /// <summary>
    /// Запрос на создание аккаунта
    /// </summary>
    public class CreateAccountRequest
    {
        /// <summary>
        /// Местоположение по умолчанию
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Местоположение по умолчанию
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Логин
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Пароль
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Повторное значение пароля
        /// </summary>
        public string PasswordConfirmation { get; set; }

        /// <summary>
        /// Контакт для авторизации
        /// </summary>
        public string AuthorizationContactValue { get; set; }
        
        /// <summary>
        /// Тип контакта для авторизации
        /// </summary>
        public Guid AuthorizationContactType { get; set; }

        /// <summary>
        /// Флаг включения отображения контакта другим пользователям
        /// </summary>
        public bool ShowContact { get; set; }
    }
}
