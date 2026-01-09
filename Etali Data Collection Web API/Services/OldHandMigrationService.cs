using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service for migrating old hand data to the current evaluated hands format.
/// </summary>
public interface IOldHandMigrationService
{
    /// <summary>
    /// Migrate eligible old hands to the evaluated hands table.
    /// Only migrates hands that don't contain problematic cards (cards with variable effects).
    /// </summary>
    /// <returns>Migration result with counts of successful, failed, and skipped records.</returns>
    Task<OldHandMigrationResult> MigrateEligibleHandsAsync();
}

/// <summary>
/// Implementation of old hand migration service.
/// </summary>
public class OldHandMigrationService : IOldHandMigrationService
{
    private readonly EtaliDatabase _db;
    private readonly GameConfiguration _gameConfig;
    private readonly ILogger<OldHandMigrationService> _logger;
    
    // Cards that have variable effects and require manual review
    private static readonly HashSet<string> ProblematicCards = new(StringComparer.OrdinalIgnoreCase)
    {
        "_____ Goblin",
        "Worldly Tutor",
        "Sylvan Tutor",
        "Gamble",
        "Gemstone Caverns"
    };
    
    // Fixed username for migrated records
    private const string ExcelImporterUsername = "excel_importer";

    public OldHandMigrationService(
        EtaliDatabase db,
        GameConfiguration gameConfig,
        ILogger<OldHandMigrationService> logger)
    {
        _db = db;
        _gameConfig = gameConfig;
        _logger = logger;
    }

    public async Task<OldHandMigrationResult> MigrateEligibleHandsAsync()
    {
        _logger.LogInformation("Starting migration of eligible old hands");

        var result = new OldHandMigrationResult();
        
        // Get the excel_importer user ID
        var excelImporterUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == ExcelImporterUsername);
        if (excelImporterUser == null)
        {
            _logger.LogError("System user '{Username}' not found. Please restart the application to create it.", ExcelImporterUsername);
            throw new InvalidOperationException($"System user '{ExcelImporterUsername}' not found. The application must be restarted to create this user.");
        }
        
        var excelImporterUserId = excelImporterUser.Id;
        _logger.LogInformation("Using system user '{Username}' (ID: {UserId}) for migrated data", ExcelImporterUsername, excelImporterUserId);
        
        // Get all old hands
        var oldHands = await _db.OldEvaluatedHands.ToListAsync();
        _logger.LogInformation("Found {Count} old hands to process", oldHands.Count);

        // Get the next available hand ID
        int nextHandId = _db.EvaluatedHands.Any() 
            ? _db.EvaluatedHands.Max(h => h.Id) + 1 
            : 1;

        foreach (var oldHand in oldHands)
        {
            try
            {
                // Check if hand contains problematic cards
                if (ContainsProblematicCard(oldHand))
                {
                    result.Skipped++;
                    _logger.LogDebug(
                        "Skipping old hand {Id} from '{DecklistName}' - contains problematic card",
                        oldHand.Id, oldHand.DecklistName);
                    continue;
                }

                // Check if decklist exists and has cards
                var decklist = await _db.Decklists.FindAsync(oldHand.DecklistId);
                if (decklist == null)
                {
                    result.Failed++;
                    _logger.LogWarning(
                        "Cannot migrate old hand {Id} - decklist ID {DecklistId} not found",
                        oldHand.Id, oldHand.DecklistId);
                    continue;
                }

                // Create the evaluated hand
                var evaluatedHand = await CreateEvaluatedHandFromOldAsync(oldHand, decklist, nextHandId, excelImporterUserId);
                
                // Add to database
                _db.EvaluatedHands.Add(evaluatedHand);
                
                // Remove from old hands
                _db.OldEvaluatedHands.Remove(oldHand);
                
                // Increment hand ID for next record
                nextHandId++;
                result.Migrated++;
                
                _logger.LogDebug(
                    "Migrated old hand {OldId} to evaluated hand {NewId} for decklist '{DecklistName}'",
                    oldHand.Id, evaluatedHand.Id, oldHand.DecklistName);
            }
            catch (Exception ex)
            {
                result.Failed++;
                _logger.LogError(ex, 
                    "Failed to migrate old hand {Id} from '{DecklistName}'",
                    oldHand.Id, oldHand.DecklistName);
            }
        }

        // Save all changes in one transaction
        try
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "Migration complete: {Migrated} migrated, {Skipped} skipped, {Failed} failed",
                result.Migrated, result.Skipped, result.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save migration changes");
            throw;
        }

        return result;
    }

    private async Task<EvaluatedHand> CreateEvaluatedHandFromOldAsync(
        OldEvaluatedHand oldHand, 
        Decklist decklist,
        int newHandId,
        string excelImporterUserId)
    {
        // Convert card names to Card objects with types from the decklist
        var cards = new Card[7];
        var cardNames = new[] 
        { 
            oldHand.Card1, oldHand.Card2, oldHand.Card3, oldHand.Card4,
            oldHand.Card5, oldHand.Card6, oldHand.Card7 
        };

        for (int i = 0; i < 7; i++)
        {
            var cardName = cardNames[i];
            if (string.IsNullOrWhiteSpace(cardName))
            {
                throw new InvalidOperationException(
                    $"Old hand {oldHand.Id} has missing card at position {i + 1}");
            }

            // Find the card in the decklist to get its type
            var cardFromDecklist = decklist.Cards.FirstOrDefault(
                c => c.Name.Equals(cardName, StringComparison.OrdinalIgnoreCase));
            
            if (cardFromDecklist == null)
            {
                // Card not in decklist - this shouldn't happen but handle gracefully
                _logger.LogWarning(
                    "Card '{CardName}' from old hand {OldHandId} not found in decklist '{DecklistName}'",
                    cardName, oldHand.Id, decklist.Name);
                
                // Use a default type
                cards[i] = new Card { Name = cardName, Type = "Unknown" };
            }
            else
            {
                cards[i] = cardFromDecklist;
            }
        }

        // Generate random values for fields that weren't tracked in old data
        var seatOrder = GenerateSeatOrder();
        var stickers = GenerateStickers();
        var lowestGambleOdds = GenerateLowestGambleOdds();

        // Create the evaluated hand
        return new EvaluatedHand(
            Id: newHandId,
            Cards: cards,
            SeatOrder: seatOrder,
            Stickers: stickers,
            CanCastEtali: oldHand.CanCastEtali,
            MinimumCardsToCastEtali: oldHand.MinimumCardsToCastEtali,
            DecklistId: oldHand.DecklistId!.Value, // Already validated it exists
            DecklistName: oldHand.DecklistName,
            LowestGambleOdds: lowestGambleOdds,
            CreatedByUserId: excelImporterUserId // System user ID for migrated data
        );
    }

    private static bool ContainsProblematicCard(OldEvaluatedHand oldHand)
    {
        var cardNames = new[] 
        { 
            oldHand.Card1, oldHand.Card2, oldHand.Card3, oldHand.Card4,
            oldHand.Card5, oldHand.Card6, oldHand.Card7 
        };

        return cardNames.Any(cardName => 
            !string.IsNullOrWhiteSpace(cardName) && 
            ProblematicCards.Contains(cardName));
    }

    private static int GenerateSeatOrder()
    {
        return Random.Shared.Next(1, 5);
    }

    private int[] GenerateStickers()
    {
        var result = new int[3];
        var indices = new HashSet<int>();

        for (int i = 0; i < 3; i++)
        {
            int index;
            do
            {
                index = Random.Shared.Next(_gameConfig.AvailableStickers.Length);
            } while (!indices.Add(index));

            result[i] = _gameConfig.AvailableStickers[index];
        }

        return result;
    }

    private static float GenerateLowestGambleOdds()
    {
        return (float)Random.Shared.NextDouble();
    }
}
