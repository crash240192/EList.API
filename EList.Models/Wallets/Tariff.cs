using System.Text.Json.Serialization;

namespace EList.Models.Wallets
{
    /// <summary>
    /// Тариф
    /// </summary>
    public class Tariff
    {
        /// <summary>
        /// Идентификатор тарифа
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название тарифа
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Оплата за период
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Полный период действия (из БД <c>interval</c>). Сохраняет минуты/часы.
        /// </summary>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// Целые дни периода (для админки / обратной совместимости JSON).
        /// Sub-day периоды дают 0 — клиент смотрит <see cref="Period"/>.
        /// При set перезаписывает <see cref="Period"/> целиком из дней.
        /// </summary>
        public int PeriodDays
        {
            get
            {
                if (Period <= TimeSpan.Zero)
                    return 0;
                return (int)Math.Floor(Period.TotalDays);
            }
            set
            {
                Period = value > 0 ? TimeSpan.FromDays(value) : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Идентификатор валидатора тарифа
        /// </summary>
        public Guid ValidatorId { get; set; }

        /// <summary>
        /// Валидатор тарифа
        /// </summary>
        public TariffValidator TariffValidator { get; set; }

        /// <summary>
        /// Тариф для организаций
        /// </summary>
        public bool ForOrganization { get; set; } = false;

        /// <summary>Период для начислений; пустой → 30 дней.</summary>
        [JsonIgnore]
        public TimeSpan EffectivePeriod =>
            Period > TimeSpan.Zero ? Period : TimeSpan.FromDays(30);

        /// <summary>Длина периода в днях (дробная), минимум 1 минута.</summary>
        [JsonIgnore]
        public double PeriodTotalDays =>
            Math.Max(EffectivePeriod.TotalDays, 1.0 / (24 * 60));
    }
}
