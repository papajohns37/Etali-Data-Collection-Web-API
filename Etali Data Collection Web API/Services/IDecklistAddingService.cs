namespace Etali_Data_Collection_Web_API.Services;

public interface IDecklistAddingService
{
    /// <summary>
    /// Adds a new decklist from a Moxfield link.
    /// </summary>
    /// <param name="link">The Moxfield URL for the decklist.</param>
    /// <param name="name">The name to assign to the decklist.</param>
    Task AddDecklist(string link, string name);
}
