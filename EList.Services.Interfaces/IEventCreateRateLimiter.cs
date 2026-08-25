namespace EList.Services.Interfaces
{
    /// <summary>
    /// In-process sliding-window rate limiter for event creation (per account).
    /// </summary>
    public interface IEventCreateRateLimiter
    {
        /// <summary>
        /// Tries to register a create attempt. Returns false if limit exceeded.
        /// </summary>
        bool TryAcquire(Guid accountId, out string? reason);
    }
}
