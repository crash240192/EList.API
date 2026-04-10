namespace EList.Services.Interfaces
{
    public interface IDebtCollectorUtility
    {
        public bool Active { get; }

        void ManualStart();
        void ManualStop();

        void Start();
        void Stop();
    }
}
