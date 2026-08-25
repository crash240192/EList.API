using System.Collections.Concurrent;
using EList.Services.Interfaces;

namespace EList.Services.Impl.AbuseProtection
{
    /// <summary>
    /// Thread-safe in-memory sliding windows for create-event abuse protection.
    /// Suitable for single-instance / sticky routing; for multi-node deploy prefer shared store.
    /// </summary>
    public sealed class EventCreateRateLimiter : IEventCreateRateLimiter
    {
        private readonly AbuseProtectionOptions _options;
        private readonly ConcurrentDictionary<Guid, SlidingWindow> _windows = new();

        public EventCreateRateLimiter(AbuseProtectionOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool TryAcquire(Guid accountId, out string? reason)
        {
            reason = null;
            if (!_options.Enabled)
                return true;

            var window = _windows.GetOrAdd(accountId, _ => new SlidingWindow());
            var now = DateTimeOffset.UtcNow;

            lock (window)
            {
                window.Prune(now, TimeSpan.FromHours(1));

                var perMinute = window.CountSince(now - TimeSpan.FromMinutes(1));
                if (perMinute >= _options.MaxCreatesPerMinute)
                {
                    reason = $"Слишком частые создания мероприятий: не более {_options.MaxCreatesPerMinute} в минуту";
                    return false;
                }

                var perHour = window.CountSince(now - TimeSpan.FromHours(1));
                if (perHour >= _options.MaxCreatesPerHour)
                {
                    reason = $"Слишком частые создания мероприятий: не более {_options.MaxCreatesPerHour} в час";
                    return false;
                }

                window.Add(now);
                return true;
            }
        }

        private sealed class SlidingWindow
        {
            private readonly List<DateTimeOffset> _hits = new();

            public void Add(DateTimeOffset ts) => _hits.Add(ts);

            public void Prune(DateTimeOffset now, TimeSpan keep)
            {
                var cutoff = now - keep;
                _hits.RemoveAll(t => t < cutoff);
            }

            public int CountSince(DateTimeOffset since)
            {
                var count = 0;
                for (var i = _hits.Count - 1; i >= 0; i--)
                {
                    if (_hits[i] >= since)
                        count++;
                    else
                        break;
                }
                return count;
            }
        }
    }
}
