namespace EList.Models.Authorization
{
    public class ResetPasswordRequest
    {
        /// <summary>
        /// Логин пользователя
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Код подтверждения сброса пароля
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Новый пароль
        /// </summary>
        public string NewPassword { get; set; }

        /// <summary>
        /// Подтверждение нового пароля
        /// </summary>
        public string NewPasswordConfirmation { get; set; }
    }
}
