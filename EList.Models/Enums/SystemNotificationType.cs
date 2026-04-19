namespace EList.Models.Enums
{
    /// <summary>
    /// Тип уведомления
    /// </summary>
    public enum SystemNotificationType
    {
        /// <summary>
        /// Создание аккаунта.
        /// </summary>
        AccountCreated = 0,

        /// <summary>
        /// Пароль был изменён.
        /// </summary>
        PasswordHasBeenChanged = 1,

        /// <summary>
        /// Простое уведомление с кодом активации.
        /// </summary>
        Activation = 2
    }
}
