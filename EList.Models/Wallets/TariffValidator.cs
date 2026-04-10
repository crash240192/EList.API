namespace EList.Models.Wallets
{
    /// <summary>
    /// Валидатор тарифа
    /// </summary>
    public class TariffValidator
    {
        /// <summary>
        /// Идентификатор валидатора
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Максимальная цена за мероприятие
        /// </summary>
        public double? CostLimit { get; set; }

        /// <summary>
        /// Максимальное количество участников
        /// </summary>
        public int? PersonsLimit { get; set; }

        /// <summary>
        /// Разрешение на закрытие мероприятия
        /// </summary>
        public bool AllowPrivate { get; set; }

        /// <summary>
        /// Возрастной диапазон
        /// </summary>
        public int? AgeLimit { get; set; }

        /// <summary>
        /// Рарзрешить отсеивание по полу
        /// </summary>
        public bool? AllowGenderSegregation { get; set; }
    }
}
