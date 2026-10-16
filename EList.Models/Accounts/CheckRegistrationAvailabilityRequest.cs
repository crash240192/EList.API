namespace EList.Models.Accounts
{
    /// <summary>
    /// Предварительная проверка уникальности логина и контакта авторизации (шаг 1 регистрации).
    /// </summary>
    public class CheckRegistrationAvailabilityRequest
    {
        /// <summary>Логин для проверки занятости.</summary>
        public string Login { get; set; }

        /// <summary>Значение контакта авторизации.</summary>
        public string AuthorizationContactValue { get; set; }

        /// <summary>Тип контакта авторизации.</summary>
        public Guid AuthorizationContactType { get; set; }
    }
}
