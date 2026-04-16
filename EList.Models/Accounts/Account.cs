using EList.Models.Person;

namespace EList.Models.Accounts
{
    /// <summary>
    /// Аккаунт
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Поле вкл/выкл
        /// </summary>
        public bool Active { get; set; }

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
        /// Хэш пароля
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Дата регистрации
        /// </summary>
        public DateTimeOffset RegistrationDate { get; set; }

        /// <summary>
        /// Дата последнего посещения
        /// </summary>
        public DateTimeOffset LastSeenDate { get; set; }

        /// <summary>
        /// Дата последнего действия
        /// </summary>
        public DateTimeOffset LastActionDate { get; set; }

        /// <summary>
        /// Идентификатор кошелька
        /// </summary>
        public Guid? WalletId { get; set; }
    }
}
