namespace Etali_Data_Collection_Web_API.Configuration;

/// <summary>
/// Configuration for Moxfield API access.
/// </summary>
public class MoxfieldConfiguration
{
    /// <summary>
    /// The base URL for the Moxfield API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api2.moxfield.com/v3/decks/all/";

    /// <summary>
    /// The API key (User-Agent) for authenticating with Moxfield.
    /// Format: "MoxKey; username passkey"
    /// This should be stored in User Secrets, not in appsettings.json.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
