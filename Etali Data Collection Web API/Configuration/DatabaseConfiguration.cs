namespace Etali_Data_Collection_Web_API.Configuration;

/// <summary>
/// Configuration for database connection and behavior.
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// Connection timeout in seconds. Default: 30 seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for transient failures. Default: 3.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Maximum delay between retries in seconds. Default: 30 seconds.
    /// </summary>
    public int MaxRetryDelay { get; set; } = 30;

    /// <summary>
    /// Enable sensitive data logging (SQL parameters). Should be false in production.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Enable detailed error messages. Should be false in production.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Maximum connection pool size. Default: 100.
    /// </summary>
    public int PoolSize { get; set; } = 100;

    /// <summary>
    /// Whether to automatically run migrations on startup (Development only).
    /// </summary>
    public bool AutoMigrate { get; set; } = false;
}
