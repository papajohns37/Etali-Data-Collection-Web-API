namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Interface for rate limiting API requests.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Waits if necessary to ensure rate limit compliance, then allows the action to proceed.
    /// </summary>
    /// <returns>A task that completes when the action is allowed to proceed.</returns>
    Task WaitIfNeededAsync();
}
