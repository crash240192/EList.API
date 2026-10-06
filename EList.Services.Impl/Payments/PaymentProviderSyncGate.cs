using System.Collections.Concurrent;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// Ограничение частоты GetState при sync-on-read (return-page poll).
    /// </summary>
    public static class PaymentProviderSyncGate
    {
        private static readonly ConcurrentDictionary<string, long> LastAttemptMs = new(StringComparer.Ordinal);

        /// <summary>True — можно звать провайдера; false — слишком рано после прошлой попытки.</summary>
        public static bool TryEnter(string providerPaymentId, int minIntervalMs = 2000)
        {
            if (string.IsNullOrWhiteSpace(providerPaymentId))
                return false;

            var key = providerPaymentId.Trim();
            var now = Environment.TickCount64;
            while (true)
            {
                if (!LastAttemptMs.TryGetValue(key, out var last))
                {
                    if (LastAttemptMs.TryAdd(key, now))
                        return true;
                    continue;
                }

                if (now - last < minIntervalMs)
                    return false;

                if (LastAttemptMs.TryUpdate(key, now, last))
                    return true;
            }
        }
    }
}
