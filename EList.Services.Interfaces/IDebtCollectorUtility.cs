namespace EList.Services.Interfaces
{
    /// <summary>
    /// Управление фоновым сборщиком задолженности.
    /// Реализация — hosted worker <c>DebtCollectorWorker</c>.
    /// </summary>
    public interface IDebtCollectorUtility
    {
        bool Active { get; }

        void ManualStart();
        void ManualStop();

        /// <summary>
        /// Совместимость со старым API: включает воркер (hosted service уже запущен хостом).
        /// </summary>
        void Start();

        /// <summary>
        /// Совместимость со старым API: отключает обработку итераций.
        /// </summary>
        void Stop();
    }
}
