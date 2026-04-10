using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Тип уведомления
    /// </summary>
    public enum SystemNotificationType
    {
        /// <summary>
        /// Создание аккаунта.
        /// </summary>
        [MapValue(Value = "account_created")]
        AccountCreated = 0,

        /// <summary>
        /// Пароль был изменён.
        /// </summary>
        [MapValue(Value = "password_has_been_changed")]
        PasswordHasBeenChanged = 1,

        /// <summary>
        /// Простое уведомление с кодом активации.
        /// </summary>
        [MapValue(Value = "new_authorization")]
        NewAuthorization = 2
    }
}
