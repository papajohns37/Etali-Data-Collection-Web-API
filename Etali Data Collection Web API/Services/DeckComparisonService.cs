using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service for managing deck comparisons and their associated evaluations.
/// </summary>
public class DeckComparisonService(
    EtaliDatabase database,
    ILogger<DeckComparisonService> logger,
    IHandGenerationService handGenerationService) : IDeckComparisonService
{
    private const double ZScore90Percent = 1.645;
    private static readonly int[] HandSizes = [7, 6, 5, 4, 3];
    private const int MaxGenerationAttempts = 1000;

    /// <inheritdoc/>
    public async Task<DeckComparison> AddDeckComparisonAsync(int deck1Id, int deck2Id)
    {
        // Validate that both decks exist
        var deck1 = await database.Decklists.FirstOrDefaultAsync(d => d.Id == deck1Id);
        var deck2 = await database.Decklists.FirstOrDefaultAsync(d => d.Id == deck2Id);

        if (deck1 == null)
        {
            throw new InvalidOperationException($"Decklist with ID {deck1Id} not found.");
        }

        if (deck2 == null)
        {
            throw new InvalidOperationException($"Decklist with ID {deck2Id} not found.");
        }

        if (deck1Id == deck2Id)
        {
            throw new InvalidOperationException("Cannot compare a deck to itself.");
        }

        // Check for existing comparison (order-independent)
        var existingComparison = await database.DeckComparisons
            .FirstOrDefaultAsync(dc => 
                (dc.Deck1Id == deck1Id && dc.Deck2Id == deck2Id) ||
                (dc.Deck1Id == deck2Id && dc.Deck2Id == deck1Id));

        if (existingComparison != null)
        {
            throw new InvalidOperationException(
                $"A comparison between deck {deck1Id} and deck {deck2Id} already exists (ID: {existingComparison.Id}).");
        }

        // Get the next ID
        int nextId = database.DeckComparisons.Any() 
            ? database.DeckComparisons.Max(dc => dc.Id) + 1 
            : 1;

        var createdAt = DateTime.UtcNow;

        // Ensure deck1Id < deck2Id to satisfy database constraint
        int orderedDeck1Id = Math.Min(deck1Id, deck2Id);
        int orderedDeck2Id = Math.Max(deck1Id, deck2Id);
        var orderedDeck1 = orderedDeck1Id == deck1Id ? deck1 : deck2;
        var orderedDeck2 = orderedDeck2Id == deck2Id ? deck2 : deck1;

        var comparison = new DeckComparison
        {
            Id = nextId,
            Deck1Id = orderedDeck1Id,
            Deck1Name = orderedDeck1.Name,
            Deck2Id = orderedDeck2Id,
            Deck2Name = orderedDeck2.Name,
            Deck1N = 0,
            Deck2N = 0,
            CreatedAt = createdAt
        };

        database.DeckComparisons.Add(comparison);
        await database.SaveChangesAsync();

        logger.LogInformation(
            "Deck comparison created: ID {ComparisonId}, Deck1 '{Deck1Name}' (ID: {Deck1Id}), Deck2 '{Deck2Name}' (ID: {Deck2Id}) at {CreatedAt:yyyy-MM-dd HH:mm:ss} UTC",
            nextId, orderedDeck1.Name, orderedDeck1Id, orderedDeck2.Name, orderedDeck2Id, createdAt);

        return comparison;
    }

    /// <inheritdoc/>
    public async Task<DeckComparison[]> GetAllComparisonsAsync()
    {
        return await database.DeckComparisons.ToArrayAsync();
    }

    /// <inheritdoc/>
    public async Task<DeckComparison?> GetComparisonByIdAsync(int comparisonId)
    {
        return await database.DeckComparisons.FirstOrDefaultAsync(dc => dc.Id == comparisonId);
    }

    /// <inheritdoc/>
    public async Task<DeckComparison[]> GetComparisonsForDeckAsync(int decklistId)
    {
        return await database.DeckComparisons
            .Where(dc => dc.Deck1Id == decklistId || dc.Deck2Id == decklistId)
            .ToArrayAsync();
    }

    /// <inheritdoc/>
    public async Task<CardDifferences?> GetCardDifferencesByDeckIdsAsync(int deck1Id, int deck2Id)
    {
        var deck1 = await database.Decklists.FirstOrDefaultAsync(d => d.Id == deck1Id);
        var deck2 = await database.Decklists.FirstOrDefaultAsync(d => d.Id == deck2Id);

        if (deck1 == null || deck2 == null)
        {
            logger.LogWarning("One or both decklists not found: Deck1Id={Deck1Id}, Deck2Id={Deck2Id}", deck1Id, deck2Id);
            return null;
        }

        // Create sets of card names for comparison
        var deck1CardNames = new HashSet<string>(deck1.Cards.Select(c => c.Name));
        var deck2CardNames = new HashSet<string>(deck2.Cards.Select(c => c.Name));

        // Find unique and shared cards
        var onlyInDeck1 = deck1.Cards
            .Where(c => !deck2CardNames.Contains(c.Name))
            .DistinctBy(c => c.Name)
            .ToArray();

        var onlyInDeck2 = deck2.Cards
            .Where(c => !deck1CardNames.Contains(c.Name))
            .DistinctBy(c => c.Name)
            .ToArray();

        var shared = deck1.Cards
            .Where(c => deck2CardNames.Contains(c.Name))
            .DistinctBy(c => c.Name)
            .ToArray();

        logger.LogInformation(
            "Card differences between Deck {Deck1Id} and Deck {Deck2Id}: {OnlyInDeck1} unique to deck1, {OnlyInDeck2} unique to deck2, {Shared} shared",
            deck1Id, deck2Id, onlyInDeck1.Length, onlyInDeck2.Length, shared.Length);

        return new CardDifferences(onlyInDeck1, onlyInDeck2, shared);
    }

    /// <summary>
    /// Gets the card differences for a specific comparison (internal helper).
    /// </summary>
    private async Task<CardDifferences?> GetCardDifferencesAsync(int comparisonId)
    {
        var comparison = await database.DeckComparisons.FirstOrDefaultAsync(dc => dc.Id == comparisonId);
        if (comparison == null)
        {
            return null;
        }

        return await GetCardDifferencesByDeckIdsAsync(comparison.Deck1Id, comparison.Deck2Id);
    }

    /// <inheritdoc/>
    public async Task<ComparisonHand?> GenerateHandForComparisonAsync(int comparisonId)
    {
        var comparison = await database.DeckComparisons.FirstOrDefaultAsync(dc => dc.Id == comparisonId);
        if (comparison == null)
        {
            return null;
        }

        // Determine which deck to generate from based on the count of comparison-specific evaluated hands
        // Always generate from the deck with fewer evaluated comparison hands to maintain balance
        var deck1ComparisonHandCount = await database.ComparisonEvaluatedHands
            .CountAsync(h => h.ComparisonId == comparisonId && h.WhichDeck == 1);
        
        var deck2ComparisonHandCount = await database.ComparisonEvaluatedHands
            .CountAsync(h => h.ComparisonId == comparisonId && h.WhichDeck == 2);

        int targetDeckId;
        int whichDeck;
        string targetDeckName;

        if (deck1ComparisonHandCount <= deck2ComparisonHandCount)
        {
            targetDeckId = comparison.Deck1Id;
            whichDeck = 1;
            targetDeckName = comparison.Deck1Name;
        }
        else
        {
            targetDeckId = comparison.Deck2Id;
            whichDeck = 2;
            targetDeckName = comparison.Deck2Name;
        }

        // Get card differences to know which cards are unique
        var cardDifferences = await GetCardDifferencesAsync(comparisonId);
        if (cardDifferences == null)
        {
            return null;
        }

        var uniqueCards = whichDeck == 1 
            ? cardDifferences.CardsOnlyInDeck1 
            : cardDifferences.CardsOnlyInDeck2;

        if (uniqueCards.Length == 0)
        {
            throw new InvalidOperationException(
                $"Deck {whichDeck} has no unique cards. Cannot generate comparison hands.");
        }

        var uniqueCardNames = new HashSet<string>(uniqueCards.Select(c => c.Name));

        // Generate the next hand ID
        int nextHandId = database.ComparisonEvaluatedHands.Any()
            ? database.ComparisonEvaluatedHands.Max(h => h.Id) + 1
            : 1;

        // Generate hands until we find one with a unique card
        int attempts = 0;
        Hand? generatedHand = null;

        while (attempts < MaxGenerationAttempts)
        {
            attempts++;
            generatedHand = handGenerationService.GenerateHand(nextHandId, targetDeckName);

            // Check if this hand contains at least one unique card
            bool hasUniqueCard = generatedHand.Cards.Any(c => uniqueCardNames.Contains(c.Name));

            if (hasUniqueCard)
            {
                logger.LogInformation(
                    "Generated hand for comparison {ComparisonId}, deck {WhichDeck} (evaluated hands: {Deck1Count} vs {Deck2Count}) after {Attempts} attempts",
                    comparisonId, whichDeck, deck1ComparisonHandCount, deck2ComparisonHandCount, attempts);

                return new ComparisonHand(
                    generatedHand.Id,
                    comparisonId,
                    whichDeck,
                    generatedHand.Cards,
                    generatedHand.SeatOrder,
                    generatedHand.Stickers,
                    generatedHand.DecklistId,
                    generatedHand.DecklistName,
                    generatedHand.LowestGambleOdds,
                    attempts);
            }
        }

        throw new InvalidOperationException(
            $"Could not generate a hand with unique cards after {MaxGenerationAttempts} attempts. " +
            $"Deck {whichDeck} may have very few unique cards.");
    }

    /// <inheritdoc/>
    public async Task<ComparisonEvaluatedHand?> SubmitComparisonEvaluationAsync(
        int comparisonId,
        int whichDeck,
        EvaluatedHand evaluatedHand,
        int generationAttempts)
    {
        var comparison = await database.DeckComparisons.FirstOrDefaultAsync(dc => dc.Id == comparisonId);
        if (comparison == null)
        {
            return null;
        }

        // Update the N value for the appropriate deck
        if (whichDeck == 1)
        {
            comparison.Deck1N += generationAttempts;
        }
        else if (whichDeck == 2)
        {
            comparison.Deck2N += generationAttempts;
        }
        else
        {
            throw new ArgumentException($"Invalid whichDeck value: {whichDeck}. Must be 1 or 2.");
        }

        // Create the comparison evaluated hand
        var comparisonEvaluatedHand = new ComparisonEvaluatedHand(
            evaluatedHand.Id,
            comparisonId,
            whichDeck,
            evaluatedHand.Cards,
            evaluatedHand.SeatOrder,
            evaluatedHand.Stickers,
            evaluatedHand.CanCastEtali,
            evaluatedHand.MinimumCardsToCastEtali,
            evaluatedHand.DecklistId,
            evaluatedHand.DecklistName,
            evaluatedHand.LowestGambleOdds,
            generationAttempts);

        database.ComparisonEvaluatedHands.Add(comparisonEvaluatedHand);
        await database.SaveChangesAsync();

        logger.LogInformation(
            "Comparison evaluation submitted: ComparisonId {ComparisonId}, Deck {WhichDeck}, HandId {HandId}, Attempts {Attempts}, New N: {NewN}",
            comparisonId, whichDeck, evaluatedHand.Id, generationAttempts,
            whichDeck == 1 ? comparison.Deck1N : comparison.Deck2N);

        return comparisonEvaluatedHand;
    }

    /// <inheritdoc/>
    public async Task<DeckComparisonStatistics?> GetComparisonStatisticsAsync(int comparisonId)
    {
        var comparison = await database.DeckComparisons.FirstOrDefaultAsync(dc => dc.Id == comparisonId);
        if (comparison == null)
        {
            return null;
        }

        // Get card differences to determine unique cards for filtering
        var cardDifferences = await GetCardDifferencesByDeckIdsAsync(comparison.Deck1Id, comparison.Deck2Id);
        if (cardDifferences == null)
        {
            return null;
        }

        var deck1UniqueCardNames = new HashSet<string>(cardDifferences.CardsOnlyInDeck1.Select(c => c.Name));
        var deck2UniqueCardNames = new HashSet<string>(cardDifferences.CardsOnlyInDeck2.Select(c => c.Name));

        // Get comparison-specific evaluated hands
        var deck1ComparisonHands = await database.ComparisonEvaluatedHands
            .Where(h => h.ComparisonId == comparisonId && h.WhichDeck == 1)
            .ToListAsync();

        var deck2ComparisonHands = await database.ComparisonEvaluatedHands
            .Where(h => h.ComparisonId == comparisonId && h.WhichDeck == 2)
            .ToListAsync();

        // Get main dataset hands for each deck, filtered to only include those with unique cards
        var deck1MainHands = await database.EvaluatedHands
            .Where(h => h.DecklistId == comparison.Deck1Id)
            .ToListAsync();

        var deck2MainHands = await database.EvaluatedHands
            .Where(h => h.DecklistId == comparison.Deck2Id)
            .ToListAsync();

        // Filter main dataset hands to only include those with at least one unique card
        var deck1FilteredMainHands = deck1MainHands
            .Where(h => h.Cards.Any(c => deck1UniqueCardNames.Contains(c.Name)))
            .ToList();

        var deck2FilteredMainHands = deck2MainHands
            .Where(h => h.Cards.Any(c => deck2UniqueCardNames.Contains(c.Name)))
            .ToList();

        // Convert filtered main hands to comparison format for unified processing
        var deck1CombinedHands = deck1ComparisonHands
            .Select(h => new CombinedHandData(h.CanCastEtali, h.MinimumCardsToCastEtali))
            .Concat(deck1FilteredMainHands
                .Select(h => new CombinedHandData(h.CanCastEtali, h.MinimumCardsToCastEtali)))
            .ToList();

        var deck2CombinedHands = deck2ComparisonHands
            .Select(h => new CombinedHandData(h.CanCastEtali, h.MinimumCardsToCastEtali))
            .Concat(deck2FilteredMainHands
                .Select(h => new CombinedHandData(h.CanCastEtali, h.MinimumCardsToCastEtali)))
            .ToList();

        // Calculate statistics for each hand size using combined data
        var handSizeDeltaStats = new List<HandSizeDeltaStatistics>();

        foreach (int handSize in HandSizes)
        {
            var deltaStats = CalculateHandSizeDeltaFromCombined(deck1CombinedHands, deck2CombinedHands, handSize);
            handSizeDeltaStats.Add(deltaStats);
        }

        // Calculate overall mulligan probability difference
        var (overallDiff, overallCILower, overallCIUpper) = CalculateOverallDifference(handSizeDeltaStats);

        logger.LogInformation(
            "Comparison statistics calculated: ComparisonId {ComparisonId}, Deck1: {D1Comp} comparison + {D1Main} filtered main hands, Deck2: {D2Comp} comparison + {D2Main} filtered main hands",
            comparisonId, deck1ComparisonHands.Count, deck1FilteredMainHands.Count,
            deck2ComparisonHands.Count, deck2FilteredMainHands.Count);

        return new DeckComparisonStatistics(
            comparisonId,
            comparison.Deck1Id,
            comparison.Deck1Name,
            comparison.Deck2Id,
            comparison.Deck2Name,
            deck1CombinedHands.Count,
            deck2CombinedHands.Count,
            [.. handSizeDeltaStats],
            overallDiff,
            overallCILower,
            overallCIUpper);
    }

    /// <summary>
    /// Simple data structure for unified hand processing.
    /// </summary>
    private record CombinedHandData(bool CanCastEtali, int MinimumCardsToCastEtali);

    private static HandSizeDeltaStatistics CalculateHandSizeDeltaFromCombined(
        List<CombinedHandData> deck1Hands,
        List<CombinedHandData> deck2Hands,
        int handSize)
    {
        int deck1Total = deck1Hands.Count;
        int deck2Total = deck2Hands.Count;

        int deck1Successful = deck1Hands.Count(h => h.CanCastEtali && h.MinimumCardsToCastEtali <= handSize);
        int deck2Successful = deck2Hands.Count(h => h.CanCastEtali && h.MinimumCardsToCastEtali <= handSize);

        double deck1Prob = deck1Total > 0 ? (double)deck1Successful / deck1Total : 0;
        double deck2Prob = deck2Total > 0 ? (double)deck2Successful / deck2Total : 0;

        double difference = deck1Prob - deck2Prob;

        // Calculate confidence interval for the difference
        var (ciLower, ciUpper) = CalculateDifferenceConfidenceInterval(
            deck1Successful, deck1Total, deck2Successful, deck2Total, ZScore90Percent);

        return new HandSizeDeltaStatistics(
            handSize,
            deck1Successful,
            deck2Successful,
            deck1Prob,
            deck2Prob,
            difference,
            ciLower,
            ciUpper);
    }

    /// <summary>
    /// Calculates the confidence interval for the difference between two proportions.
    /// Uses the standard error of the difference between two independent proportions.
    /// </summary>
    private static (double lower, double upper) CalculateDifferenceConfidenceInterval(
        int successes1, int total1, int successes2, int total2, double zScore)
    {
        if (total1 == 0 || total2 == 0)
        {
            return (0, 0);
        }

        double p1 = (double)successes1 / total1;
        double p2 = (double)successes2 / total2;
        double difference = p1 - p2;

        // Standard error of the difference
        double se = Math.Sqrt((p1 * (1 - p1) / total1) + (p2 * (1 - p2) / total2));

        double marginOfError = zScore * se;

        double lower = difference - marginOfError;
        double upper = difference + marginOfError;

        // Clamp to valid range [-1, 1]
        lower = Math.Max(-1, Math.Min(1, lower));
        upper = Math.Max(-1, Math.Min(1, upper));

        return (lower, upper);
    }

    /// <summary>
    /// Calculates the overall mulligan probability difference and confidence interval.
    /// Uses the same formula as individual decklist statistics: 1 - (1 - p7)^2 * (1 - p6) * (1 - p5) * (1 - p4) * (1 - p3)
    /// Then takes the difference between deck1 and deck2 overall probabilities.
    /// 
    /// Confidence interval propagation:
    /// Since the per-hand-size CIs are for differences (deck1 - deck2), we need to extract
    /// individual deck CIs first. However, this is complex for correlated samples.
    /// 
    /// CORRECTED APPROACH:
    /// Calculate separate overall probabilities and CIs for each deck independently,
    /// then compute the difference and its CI using variance propagation.
    /// </summary>
    private static (double difference, double ciLower, double ciUpper) CalculateOverallDifference(
        List<HandSizeDeltaStatistics> handSizeStats)
    {
        // Extract statistics for each hand size
        var statsDict = handSizeStats.ToDictionary(s => s.HandSize);
        var s7 = statsDict[7];
        var s6 = statsDict[6];
        var s5 = statsDict[5];
        var s4 = statsDict[4];
        var s3 = statsDict[3];

        // Calculate overall probability for deck 1 (point estimate)
        double overall1 = 1 - Math.Pow(1 - s7.Deck1Probability, 2) * 
                              (1 - s6.Deck1Probability) * 
                              (1 - s5.Deck1Probability) * 
                              (1 - s4.Deck1Probability) * 
                              (1 - s3.Deck1Probability);

        // Calculate overall probability for deck 2 (point estimate)
        double overall2 = 1 - Math.Pow(1 - s7.Deck2Probability, 2) * 
                              (1 - s6.Deck2Probability) * 
                              (1 - s5.Deck2Probability) * 
                              (1 - s4.Deck2Probability) * 
                              (1 - s3.Deck2Probability);

        double difference = overall1 - overall2;

        // CORRECTED CI CALCULATION:
        // We need to propagate CIs through the nonlinear mulligan formula separately for each deck.
        // Since we don't have individual deck CIs directly, we use a conservative approximation:
        // The difference CI width should be at least as wide as the individual hand size CIs suggest.
        
        // For deck1, extract implied CIs from the difference CIs
        // If CI_diff = [L, U] and diff = p1 - p2, then:
        // At the lower bound: p1 - p2 = L, so p1 = p2 + L
        // At the upper bound: p1 - p2 = U, so p1 = p2 + U
        // For lower CI of overall, use pessimistic values (lower for deck1, upper for deck2)
        // For upper CI of overall, use optimistic values (upper for deck1, lower for deck2)
        
        // Calculate deck 1 pessimistic (lower bound) probabilities
        double p1_7_lower = Math.Max(0, s7.Deck1Probability - Math.Abs(s7.ProbabilityDifference - s7.ConfidenceIntervalLower));
        double p1_6_lower = Math.Max(0, s6.Deck1Probability - Math.Abs(s6.ProbabilityDifference - s6.ConfidenceIntervalLower));
        double p1_5_lower = Math.Max(0, s5.Deck1Probability - Math.Abs(s5.ProbabilityDifference - s5.ConfidenceIntervalLower));
        double p1_4_lower = Math.Max(0, s4.Deck1Probability - Math.Abs(s4.ProbabilityDifference - s4.ConfidenceIntervalLower));
        double p1_3_lower = Math.Max(0, s3.Deck1Probability - Math.Abs(s3.ProbabilityDifference - s3.ConfidenceIntervalLower));

        // Calculate deck 1 optimistic (upper bound) probabilities
        double p1_7_upper = Math.Min(1, s7.Deck1Probability + Math.Abs(s7.ConfidenceIntervalUpper - s7.ProbabilityDifference));
        double p1_6_upper = Math.Min(1, s6.Deck1Probability + Math.Abs(s6.ConfidenceIntervalUpper - s6.ProbabilityDifference));
        double p1_5_upper = Math.Min(1, s5.Deck1Probability + Math.Abs(s5.ConfidenceIntervalUpper - s5.ProbabilityDifference));
        double p1_4_upper = Math.Min(1, s4.Deck1Probability + Math.Abs(s4.ConfidenceIntervalUpper - s4.ProbabilityDifference));
        double p1_3_upper = Math.Min(1, s3.Deck1Probability + Math.Abs(s3.ConfidenceIntervalUpper - s3.ProbabilityDifference));

        // Calculate deck 2 pessimistic (lower bound) probabilities
        double p2_7_lower = Math.Max(0, s7.Deck2Probability - Math.Abs(s7.ConfidenceIntervalUpper - s7.ProbabilityDifference));
        double p2_6_lower = Math.Max(0, s6.Deck2Probability - Math.Abs(s6.ConfidenceIntervalUpper - s6.ProbabilityDifference));
        double p2_5_lower = Math.Max(0, s5.Deck2Probability - Math.Abs(s5.ConfidenceIntervalUpper - s5.ProbabilityDifference));
        double p2_4_lower = Math.Max(0, s4.Deck2Probability - Math.Abs(s4.ConfidenceIntervalUpper - s4.ProbabilityDifference));
        double p2_3_lower = Math.Max(0, s3.Deck2Probability - Math.Abs(s3.ConfidenceIntervalUpper - s3.ProbabilityDifference));

        // Calculate deck 2 optimistic (upper bound) probabilities
        double p2_7_upper = Math.Min(1, s7.Deck2Probability + Math.Abs(s7.ProbabilityDifference - s7.ConfidenceIntervalLower));
        double p2_6_upper = Math.Min(1, s6.Deck2Probability + Math.Abs(s6.ProbabilityDifference - s6.ConfidenceIntervalLower));
        double p2_5_upper = Math.Min(1, s5.Deck2Probability + Math.Abs(s5.ProbabilityDifference - s5.ConfidenceIntervalLower));
        double p2_4_upper = Math.Min(1, s4.Deck2Probability + Math.Abs(s4.ProbabilityDifference - s4.ConfidenceIntervalLower));
        double p2_3_upper = Math.Min(1, s3.Deck2Probability + Math.Abs(s3.ProbabilityDifference - s3.ConfidenceIntervalLower));

        // Calculate overall probabilities with CI bounds using the mulligan formula
        double overall1Lower = 1 - Math.Pow(1 - p1_7_lower, 2) * (1 - p1_6_lower) * (1 - p1_5_lower) * (1 - p1_4_lower) * (1 - p1_3_lower);
        double overall1Upper = 1 - Math.Pow(1 - p1_7_upper, 2) * (1 - p1_6_upper) * (1 - p1_5_upper) * (1 - p1_4_upper) * (1 - p1_3_upper);
        
        double overall2Lower = 1 - Math.Pow(1 - p2_7_lower, 2) * (1 - p2_6_lower) * (1 - p2_5_lower) * (1 - p2_4_lower) * (1 - p2_3_lower);
        double overall2Upper = 1 - Math.Pow(1 - p2_7_upper, 2) * (1 - p2_6_upper) * (1 - p2_5_upper) * (1 - p2_4_upper) * (1 - p2_3_upper);

        // Difference CI bounds:
        // Lower bound: deck1 at its worst - deck2 at its best
        // Upper bound: deck1 at its best - deck2 at its worst
        double ciLower = overall1Lower - overall2Upper;
        double ciUpper = overall1Upper - overall2Lower;

        // Clamp final CI to valid range [-1, 1]
        ciLower = Math.Max(-1, Math.Min(1, ciLower));
        ciUpper = Math.Max(-1, Math.Min(1, ciUpper));

        return (difference, ciLower, ciUpper);
    }
}
