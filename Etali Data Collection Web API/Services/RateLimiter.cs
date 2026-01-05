namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Rate limiter that enforces a minimum time interval between requests.
/// Thread-safe implementation using SemaphoreSlim.
/// </summary>
public class RateLimiter(TimeSpan minimumInterval) : IRateLimiter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DateTime _lastRequestTime = DateTime.MinValue;

    /// <inheritdoc/>
    public async Task WaitIfNeededAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            
            if (timeSinceLastRequest < minimumInterval)
            {
                var delay = minimumInterval - timeSinceLastRequest;
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
