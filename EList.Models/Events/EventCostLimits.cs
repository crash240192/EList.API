namespace EList.Models.Events
{
    /// <summary>
    /// Абсолютные лимиты стоимости мероприятия (₽).
    /// Согласованы с лимитом пополнения кошелька.
    /// </summary>
    public static class EventCostLimits
    {
        public const double Max = 1_000_000;
    }
}
