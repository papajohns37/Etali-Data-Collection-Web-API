namespace Etali_Data_Collection_Web_API.Configuration;

/// <summary>
/// Configuration for JWT token generation and validation.
/// </summary>
public class JwtConfiguration
{
    /// <summary>
    /// Secret key used for signing JWT tokens.
    /// Must be at least 256 bits (32 characters) for HMAC-SHA256.
    /// </summary>
    public required string SecretKey { get; set; }
    
    /// <summary>
    /// Token issuer (typically your API domain/name).
    /// </summary>
    public required string Issuer { get; set; }
    
    /// <summary>
    /// Token audience (typically your API domain/name or client app).
    /// </summary>
    public required string Audience { get; set; }
    
    /// <summary>
    /// Token expiration time in days.
    /// Default: 7 days.
    /// </summary>
    public int ExpirationInDays { get; set; } = 7;
}
