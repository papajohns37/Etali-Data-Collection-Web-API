namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Rate limiter that enforces a minimum time interval between requests.
/// Thread-safe implementation using SemaphoreSlim.
/// </summary>
public class RateLimiter : IRateLimiter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _minimumInterval;
    private DateTime _lastRequestTime = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiter"/> class.
    /// </summary>
    /// <param name="minimumInterval">The minimum time interval between requests.</param>
    public RateLimiter(TimeSpan minimumInterval)
    {
        _minimumInterval = minimumInterval;
    }

    /// <inheritdoc/>
    public async Task WaitIfNeededAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            
            if (timeSinceLastRequest < _minimumInterval)
            {
                var delay = _minimumInterval - timeSinceLastRequest;
                await Task.Delay(delay);
            }
            
            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
