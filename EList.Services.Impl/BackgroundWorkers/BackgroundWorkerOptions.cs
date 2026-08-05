namespace EList.Services.Impl.BackgroundWorkers
{
    /// <summary>
    /// Общие настройки периодического фонового воркера.
    /// </summary>
    public class BackgroundWorkerOptions
    {
        /// <summary>
        /// Включён ли воркер.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Интервал запуска итерации в минутах.
        /// </summary>
        public int ProcessIntervalMinutes { get; set; } = 10;

        /// <summary>
        /// Задержка между обработкой отдельных элементов внутри итерации (мс).
        /// </summary>
        public int ItemDelayMilliseconds { get; set; } = 1000;
    }
}
