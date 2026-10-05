using System.Text.Json.Serialization;
using EList.Models.Enums;

namespace EList.Models.Person
{
    /// <summary>
    /// Информация о пользователе
    /// </summary>
    public class PersonInfo
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Идентификатор аккаунта
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Фамилия
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Отчество
        /// </summary>
        public string Patronymic { get; set; }

        /// <summary>
        /// Пол
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Дата рождения (полная — только владельцу профиля).
        /// </summary>
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// Возраст в полных годах (без даты), если разрешено настройками приватности.
        /// </summary>
        public int? AgeYears { get; set; }

        /// <summary>
        /// Сегодня день рождения владельца (для визуального акцента), без раскрытия даты.
        /// </summary>
        public bool? IsBirthdayToday { get; set; }

        [JsonIgnore]
        public string FIO
        {
            get
            {
                return $"{LastName} {FirstName}";
            }
        }
    }
}
