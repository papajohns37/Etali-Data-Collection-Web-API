using System.Collections.Concurrent;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service for managing full mulligan sessions where users test the complete mulligan process
/// to determine if they can find a hand that casts Etali by turn 2.
/// </summary>
public class FullMulliganService(
    EtaliDatabase database,
    GameConfiguration configuration,
    IHandEvaluationService handEvaluationService,
    ILogger<FullMulliganService> logger) : IFullMulliganService
{
    // Store shuffled decks in memory during active sessions
    // In production, consider using Redis or another distributed cache
    private static readonly ConcurrentDictionary<int, Card[]> _activeDecks = new();
    private static readonly ConcurrentDictionary<int, (int seatOrder, int[] stickers)> _sessionMetadata = new();
    private static readonly ConcurrentDictionary<int, List<Card>> _exiledCards = new();

    /// <inheritdoc/>
    public async Task<FullMulliganStartResponse> StartSessionAsync(int decklistId, string userId)
    {
        logger.LogInformation("Starting mulligan session - DecklistId: {DecklistId}, UserId: {UserId}", decklistId, userId);

        // Validate decklist exists
        var decklist = await database.Decklists.FirstOrDefaultAsync(d => d.Id == decklistId);
        if (decklist == null)
        {
            logger.LogWarning("Decklist not found - DecklistId: {DecklistId}", decklistId);
            throw new InvalidOperationException($"Decklist with ID {decklistId} not found.");
        }

        if (decklist.Cards.Length < 7)
        {
            logger.LogWarning("Insufficient cards in decklist - DecklistId: {DecklistId}, CardCount: {CardCount}", decklistId, decklist.Cards.Length);
            throw new InvalidOperationException(
                $"Decklist '{decklist.Name}' has insufficient cards. At least 7 cards are required.");
        }

        // Check for existing active sessions (log but don't modify)
        var activeSessions = await database.FullMulliganSessions
            .Where(s => s.DecklistId == decklistId && 
                        s.CreatedByUserId == userId && 
                        s.IsActive)
            .ToListAsync();

        if (activeSessions.Any())
        {
            logger.LogWarning(
                "User has {Count} active session(s) for decklist - DecklistId: {DecklistId}, UserId: {UserId}, SessionIds: {SessionIds}",
                activeSessions.Count,
                decklistId,
                userId,
                string.Join(", ", activeSessions.Select(s => s.Id)));

            // Check if these sessions exist in memory
            foreach (var activeSession in activeSessions)
            {
                var hasMetadata = _sessionMetadata.ContainsKey(activeSession.Id);
                var hasDeck = _activeDecks.ContainsKey(activeSession.Id);
                var hasExiled = _exiledCards.ContainsKey(activeSession.Id);

                logger.LogWarning(
                    "Active session state - SessionId: {SessionId}, InMemoryMetadata: {HasMetadata}, InMemoryDeck: {HasDeck}, InMemoryExiled: {HasExiled}, CreatedAt: {CreatedAt}, TotalMulligans: {TotalMulligans}",
                    activeSession.Id,
                    hasMetadata,
                    hasDeck,
                    hasExiled,
                    activeSession.CreatedAt,
                    activeSession.TotalMulligans);
            }
        }

        // Generate game metadata (stays consistent throughout the session)
        var seatOrder = GenerateSeatOrder();
        var stickers = GenerateStickers();

        // Create new session
        var session = new FullMulliganSession
        {
            DecklistId = decklistId,
            DecklistName = decklist.Name,
            SeatOrder = seatOrder,
            Stickers = stickers,
            FoundSuccessfulHand = false,
            FinalHandSize = 0,
            TotalMulligans = 0,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        database.FullMulliganSessions.Add(session);
        await database.SaveChangesAsync();

        // Store metadata for this session (seatOrder and stickers stay consistent)
        _sessionMetadata[session.Id] = (seatOrder, stickers);

        // Initialize exiled cards list for this session
        _exiledCards[session.Id] = new List<Card>();

        // Shuffle deck and store in memory
        var shuffledDeck = ShuffleDeck(decklist.Cards);
        _activeDecks[session.Id] = shuffledDeck;

        // Draw first 7 cards
        var firstHand = DrawHand(shuffledDeck);
        var handId = handEvaluationService.GetNextHandId();

        // Generate unique gamble odds for this hand
        var lowestGambleOdds = GenerateLowestGambleOdds();

        var hand = new Hand(
            handId,
            firstHand,
            seatOrder,
            stickers,
            decklistId,
            decklist.Name,
            lowestGambleOdds
        );

        return new FullMulliganStartResponse(
            SessionId: session.Id,
            CurrentHand: hand,
            HandNumber: 1,
            TargetHandSize: 7,
            CanMulligan: true
        );
    }

    /// <inheritdoc/>
    public async Task<FullMulliganEvaluationResponse> EvaluateHandAsync(
        FullMulliganEvaluationRequest request,
        string userId)
    {
        // Validate session exists and belongs to user
        var session = await database.FullMulliganSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId);

        if (session == null)
        {
            throw new InvalidOperationException($"Session with ID {request.SessionId} not found.");
        }

        if (session.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this session.");
        }

        if (!session.IsActive)
        {
            throw new InvalidOperationException("This session has already been completed.");
        }

        // Get decklist
        var decklist = await database.Decklists.FirstOrDefaultAsync(d => d.Id == session.DecklistId);
        if (decklist == null)
        {
            throw new InvalidOperationException($"Decklist with ID {session.DecklistId} not found.");
        }

        // Get session metadata
        if (!_sessionMetadata.TryGetValue(session.Id, out var metadata))
        {
            logger.LogError(
                "Session metadata not found in memory - SessionId: {SessionId}, DecklistId: {DecklistId}, UserId: {UserId}, IsActive: {IsActive}, CreatedAt: {CreatedAt}, HandNumber: {HandNumber}",
                session.Id,
                session.DecklistId,
                userId,
                session.IsActive,
                session.CreatedAt,
                request.HandNumber);

            throw new InvalidOperationException("Session metadata not found. Session may have expired.");
        }

        // Get current deck state
        if (!_activeDecks.TryGetValue(session.Id, out var currentDeck))
        {
            logger.LogError(
                "Session deck not found in memory - SessionId: {SessionId}, DecklistId: {DecklistId}, UserId: {UserId}, HasMetadata: true, HandNumber: {HandNumber}",
                session.Id,
                session.DecklistId,
                userId,
                request.HandNumber);

            throw new InvalidOperationException("Session deck not found. Session may have expired.");
        }

        // Get exiled cards for this session
        if (!_exiledCards.TryGetValue(session.Id, out var exiledCards))
        {
            logger.LogError(
                "Session exiled cards not found in memory - SessionId: {SessionId}, DecklistId: {DecklistId}, UserId: {UserId}, HasMetadata: true, HasDeck: true, HandNumber: {HandNumber}",
                session.Id,
                session.DecklistId,
                userId,
                request.HandNumber);

            throw new InvalidOperationException("Session exiled cards not found. Session may have expired.");
        }

        // Calculate current target hand size based on hand number
        int targetHandSize = CalculateTargetHandSize(request.HandNumber);

        // Validate hand number matches expected sequence
        if (request.HandNumber != session.TotalMulligans + 1)
        {
            throw new InvalidOperationException(
                $"Invalid hand number. Expected hand {session.TotalMulligans + 1}, got {request.HandNumber}.");
        }

        // Handle Serum Powder activation
        if (request.ActivateSerumPowder == true)
        {
            return await HandleSerumPowderActivationAsync(
                request, session, decklist, metadata, currentDeck, exiledCards, targetHandSize);
        }

        // Store this hand evaluation
        var evaluation = new MulliganHandEvaluation
        {
            SessionId = request.SessionId,
            HandNumber = request.HandNumber,
            TargetHandSize = targetHandSize,
            Cards = currentDeck.Take(7).ToArray(),
            CanCastEtaliByTurn2 = request.CanCastEtaliByTurn2,
            KeptHand = request.KeepHand,
            EvaluatedAt = DateTime.UtcNow
        };

        database.MulliganHandEvaluations.Add(evaluation);

        // If user keeps this hand or we're at 3 cards, session is complete
        if (request.KeepHand || targetHandSize == 3)
        {
            session.FoundSuccessfulHand = request.CanCastEtaliByTurn2;
            session.FinalHandSize = targetHandSize;
            session.TotalMulligans = request.HandNumber - 1;
            session.IsActive = false;
            session.CompletedAt = DateTime.UtcNow;

            logger.LogInformation(
                "Completing mulligan session - SessionId: {SessionId}, DecklistId: {DecklistId}, UserId: {UserId}, Success: {Success}, FinalHandSize: {FinalHandSize}, TotalMulligans: {TotalMulligans}, TotalSerumPowderActivations: {SerumPowder}",
                session.Id,
                session.DecklistId,
                userId,
                session.FoundSuccessfulHand,
                session.FinalHandSize,
                session.TotalMulligans,
                session.TotalSerumPowderActivations);

            await database.SaveChangesAsync();

            logger.LogInformation(
                "Session saved to database - SessionId: {SessionId}, IsActive: {IsActive}, CompletedAt: {CompletedAt}",
                session.Id,
                session.IsActive,
                session.CompletedAt);

            // Clean up memory
            _activeDecks.TryRemove(session.Id, out _);
            _sessionMetadata.TryRemove(session.Id, out _);
            _exiledCards.TryRemove(session.Id, out _);

            logger.LogInformation(
                "Session memory cleaned up - SessionId: {SessionId}",
                session.Id);

            var summary = new FullMulliganSessionSummary(
                SessionId: session.Id,
                FoundSuccessfulHand: session.FoundSuccessfulHand,
                FinalHandSize: session.FinalHandSize,
                TotalMulligans: session.TotalMulligans,
                TotalSerumPowderActivations: session.TotalSerumPowderActivations
            );

            return new FullMulliganEvaluationResponse(
                SessionComplete: true,
                NextHand: null,
                NextHandNumber: null,
                NextTargetHandSize: null,
                CanMulligan: false,
                Summary: summary
            );
        }

        // User is mulliganing - shuffle deck (excluding exiled cards) and draw new hand
        var availableCards = decklist.Cards.Where(c => !exiledCards.Any(ec => CardsEqual(ec, c))).ToArray();
        var shuffledDeck = ShuffleDeck(availableCards);
        _activeDecks[session.Id] = shuffledDeck;

        var nextHandCards = DrawHand(shuffledDeck);
        var nextHandNumber = request.HandNumber + 1;
        var nextTargetHandSize = CalculateTargetHandSize(nextHandNumber);

        // Generate NEW gamble odds for this mulligan hand
        var nextGambleOdds = GenerateLowestGambleOdds();

        var handId = handEvaluationService.GetNextHandId();
        var nextHand = new Hand(
            handId,
            nextHandCards,
            metadata.seatOrder,
            metadata.stickers,
            session.DecklistId,
            session.DecklistName,
            nextGambleOdds
        );

        // Update session mulligan count
        session.TotalMulligans = nextHandNumber - 1;
        await database.SaveChangesAsync();

        return new FullMulliganEvaluationResponse(
            SessionComplete: false,
            NextHand: nextHand,
            NextHandNumber: nextHandNumber,
            NextTargetHandSize: nextTargetHandSize,
            CanMulligan: nextTargetHandSize > 3,
            Summary: null
        );
    }

    /// <inheritdoc/>
    public async Task<FullMulliganStatistics> GetStatisticsAsync(int decklistId)
    {
        var decklist = await database.Decklists.FirstOrDefaultAsync(d => d.Id == decklistId);
        if (decklist == null)
        {
            throw new InvalidOperationException($"Decklist with ID {decklistId} not found.");
        }

        var sessions = await database.FullMulliganSessions
            .Where(s => s.DecklistId == decklistId && !s.IsActive)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            return new FullMulliganStatistics(
                DecklistId: decklistId,
                DecklistName: decklist.Name,
                TotalSessions: 0,
                SuccessfulSessions: 0,
                SuccessRate: 0,
                AverageMulligans: 0,
                SuccessByHandSize: new Dictionary<int, int>(),
                MulliganDistribution: new Dictionary<int, int>(),
                TotalSerumPowderActivations: 0,
                AverageSerumPowderActivations: 0,
                SessionsWithSerumPowder: 0,
                SuccessRateWithSerumPowder: 0,
                SuccessRateWithoutSerumPowder: 0
            );
        }

        var successfulSessions = sessions.Count(s => s.FoundSuccessfulHand);
        var successRate = (double)successfulSessions / sessions.Count * 100;
        var averageMulligans = sessions.Average(s => s.TotalMulligans);

        // Success by hand size (only for successful sessions)
        var successByHandSize = sessions
            .Where(s => s.FoundSuccessfulHand)
            .GroupBy(s => s.FinalHandSize)
            .ToDictionary(g => g.Key, g => g.Count());

        // Mulligan distribution (all sessions)
        var mulliganDistribution = sessions
            .GroupBy(s => s.TotalMulligans)
            .ToDictionary(g => g.Key, g => g.Count());

        // Serum Powder statistics
        var totalSerumPowderActivations = sessions.Sum(s => s.TotalSerumPowderActivations);
        var averageSerumPowderActivations = sessions.Average(s => s.TotalSerumPowderActivations);
        var sessionsWithSerumPowder = sessions.Count(s => s.TotalSerumPowderActivations > 0);

        var sessionsWithSP = sessions.Where(s => s.TotalSerumPowderActivations > 0).ToList();
        var sessionsWithoutSP = sessions.Where(s => s.TotalSerumPowderActivations == 0).ToList();

        var successRateWithSP = sessionsWithSP.Count > 0 
            ? (double)sessionsWithSP.Count(s => s.FoundSuccessfulHand) / sessionsWithSP.Count * 100 
            : 0;
        var successRateWithoutSP = sessionsWithoutSP.Count > 0 
            ? (double)sessionsWithoutSP.Count(s => s.FoundSuccessfulHand) / sessionsWithoutSP.Count * 100 
            : 0;

        return new FullMulliganStatistics(
            DecklistId: decklistId,
            DecklistName: decklist.Name,
            TotalSessions: sessions.Count,
            SuccessfulSessions: successfulSessions,
            SuccessRate: successRate,
            AverageMulligans: averageMulligans,
            SuccessByHandSize: successByHandSize,
            MulliganDistribution: mulliganDistribution,
            TotalSerumPowderActivations: totalSerumPowderActivations,
            AverageSerumPowderActivations: averageSerumPowderActivations,
            SessionsWithSerumPowder: sessionsWithSerumPowder,
            SuccessRateWithSerumPowder: successRateWithSP,
            SuccessRateWithoutSerumPowder: successRateWithoutSP
        );
    }

    /// <inheritdoc/>
    public async Task<List<FullMulliganStatistics>> GetAllStatisticsAsync()
    {
        var decklistIds = await database.FullMulliganSessions
            .Where(s => !s.IsActive)
            .Select(s => s.DecklistId)
            .Distinct()
            .ToListAsync();

        var statisticsList = new List<FullMulliganStatistics>();

        foreach (var decklistId in decklistIds)
        {
            var stats = await GetStatisticsAsync(decklistId);
            statisticsList.Add(stats);
        }

        return statisticsList.OrderByDescending(s => s.TotalSessions).ToList();
    }

    /// <inheritdoc/>
    public async Task<FullMulliganComparisonResult> CompareDecksAsync(int decklistId1, int decklistId2)
    {
        var stats1 = await GetStatisticsAsync(decklistId1);
        var stats2 = await GetStatisticsAsync(decklistId2);

        if (stats1.TotalSessions == 0 || stats2.TotalSessions == 0)
        {
            throw new InvalidOperationException(
                "Both decklists must have at least one completed mulligan session for comparison.");
        }

        var successRateDifference = stats1.SuccessRate - stats2.SuccessRate;
        var testResult = CalculateTwoProportionTest(
            stats1.SuccessfulSessions,
            stats1.TotalSessions,
            stats2.SuccessfulSessions,
            stats2.TotalSessions,
            stats1.DecklistName,
            stats2.DecklistName
        );

        return new FullMulliganComparisonResult(
            Decklist1Stats: stats1,
            Decklist2Stats: stats2,
            SuccessRateDifference: successRateDifference,
            StatisticalTest: testResult
        );
    }

    #region Private Helper Methods

    /// <summary>
    /// Calculates the target hand size based on hand number.
    /// Hand 1-2: 7 cards
    /// Hand 3: 6 cards
    /// Hand 4: 5 cards
    /// Hand 5: 4 cards
    /// Hand 6: 3 cards
    /// </summary>
    private static int CalculateTargetHandSize(int handNumber)
    {
        return handNumber switch
        {
            1 => 7,
            2 => 7,
            3 => 6,
            4 => 5,
            5 => 4,
            6 => 3,
            _ => throw new InvalidOperationException($"Invalid hand number: {handNumber}. Must be 1-6.")
        };
    }

    /// <summary>
    /// Shuffles a deck of cards using Fisher-Yates algorithm.
    /// </summary>
    private static Card[] ShuffleDeck(Card[] cards)
    {
        var shuffled = cards.ToArray();
        var n = shuffled.Length;

        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }

    /// <summary>
    /// Draws a specified number of cards from the top of the deck.
    /// </summary>
    /// <param name="deck">The shuffled deck to draw from.</param>
    /// <param name="count">The number of cards to draw (default is 7).</param>
    private static Card[] DrawHand(Card[] deck, int count = 7)
    {
        return deck.Take(count).ToArray();
    }

    /// <summary>
    /// Generates a random seat order position for the player.
    /// </summary>
    private static int GenerateSeatOrder()
    {
        return Random.Shared.Next(1, 5);
    }

    /// <summary>
    /// Generates 3 random sticker values from the available pool.
    /// </summary>
    private int[] GenerateStickers()
    {
        var result = new int[3];
        var indices = new HashSet<int>();

        for (int i = 0; i < 3; i++)
        {
            int index;
            do
            {
                index = Random.Shared.Next(configuration.AvailableStickers.Length);
            } while (!indices.Add(index));

            result[i] = configuration.AvailableStickers[index];
        }

        return result;
    }

    /// <summary>
    /// Generates a random Gamble odds value between 0.0 and 1.0.
    /// </summary>
    private static double GenerateLowestGambleOdds()
    {
        return Random.Shared.NextDouble();
    }

    /// <summary>
    /// Calculates a two-proportion z-test to compare success rates between two groups.
    /// </summary>
    private static TwoProportionTestResult CalculateTwoProportionTest(
        int successes1,
        int total1,
        int successes2,
        int total2,
        string name1,
        string name2)
    {
        double p1 = (double)successes1 / total1;
        double p2 = (double)successes2 / total2;

        // Pooled proportion
        double pPooled = (double)(successes1 + successes2) / (total1 + total2);

        // Standard error
        double se = Math.Sqrt(pPooled * (1 - pPooled) * (1.0 / total1 + 1.0 / total2));

        // Z-score
        double zScore = (p1 - p2) / se;

        // Two-tailed p-value using standard normal distribution
        double pValue = 2 * (1 - StandardNormalCdf(Math.Abs(zScore)));

        bool isSignificant = pValue < 0.05;

        // Generate interpretation
        string interpretation;
        if (!isSignificant)
        {
            interpretation = $"No statistically significant difference found between '{name1}' and '{name2}' " +
                           $"(p = {pValue:F3}). The observed difference in success rates could be due to random chance.";
        }
        else
        {
            var higherDeck = p1 > p2 ? name1 : name2;
            var lowerDeck = p1 > p2 ? name2 : name1;
            interpretation = $"Deck '{higherDeck}' has a statistically significantly higher success rate than " +
                           $"'{lowerDeck}' (p = {pValue:F3}).";
        }

        return new TwoProportionTestResult(
            ZScore: zScore,
            PValue: pValue,
            IsSignificant: isSignificant,
            Interpretation: interpretation
        );
    }

    /// <summary>
    /// Calculates the cumulative distribution function for the standard normal distribution.
    /// Uses approximation for computational efficiency.
    /// </summary>
    private static double StandardNormalCdf(double x)
    {
        // Using the error function approximation
        // CDF(x) = 0.5 * (1 + erf(x / sqrt(2)))
        return 0.5 * (1 + Erf(x / Math.Sqrt(2)));
    }

    /// <summary>
    /// Approximates the error function using Abramowitz and Stegun formula.
    /// Maximum error: 1.5×10^−7
    /// </summary>
    private static double Erf(double x)
    {
        // Constants
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        // Save the sign of x
        int sign = x < 0 ? -1 : 1;
        x = Math.Abs(x);

        // A&S formula
        double t = 1.0 / (1.0 + p * x);
        double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

        return sign * y;
    }

    /// <summary>
    /// Handles Serum Powder activation during a mulligan session.
    /// </summary>
    private async Task<FullMulliganEvaluationResponse> HandleSerumPowderActivationAsync(
        FullMulliganEvaluationRequest request,
        FullMulliganSession session,
        Decklist decklist,
        (int seatOrder, int[] stickers) metadata,
        Card[] currentDeck,
        List<Card> exiledCards,
        int targetHandSize)
    {
        // Validate CardsToBottomIndices is provided (null is invalid, empty array is valid for 7-card hands)
        if (request.CardsToBottomIndices == null)
        {
            throw new InvalidOperationException("CardsToBottomIndices must be provided when activating Serum Powder (use empty array for 7-card hands).");
        }

        // Calculate expected number of cards to bottom
        int expectedBottomCount = 7 - targetHandSize;

        if (request.CardsToBottomIndices.Length != expectedBottomCount)
        {
            throw new InvalidOperationException(
                $"Must bottom exactly {expectedBottomCount} card(s) for hand size {targetHandSize}. " +
                $"Received {request.CardsToBottomIndices.Length} indices.");
        }

        // Validate indices are in range [0-6]
        if (request.CardsToBottomIndices.Any(i => i < 0 || i > 6))
        {
            throw new InvalidOperationException("Card indices must be between 0 and 6.");
        }

        // Validate indices are unique
        if (request.CardsToBottomIndices.Distinct().Count() != request.CardsToBottomIndices.Length)
        {
            throw new InvalidOperationException("Card indices must be unique.");
        }

        // Get current hand
        var currentHand = currentDeck.Take(7).ToArray();

        // Get cards to bottom
        var cardsToBottom = request.CardsToBottomIndices
            .Select(i => currentHand[i])
            .ToArray();

        // Get cards to exile (remaining cards in hand after bottoming)
        var cardsToExile = currentHand
            .Where((card, index) => !request.CardsToBottomIndices.Contains(index))
            .ToArray();

        // Add cards to exiled list
        exiledCards.AddRange(cardsToExile);

        // Store activation in database
        var activation = new SerumPowderActivation
        {
            SessionId = session.Id,
            HandNumber = request.HandNumber,
            TargetHandSize = targetHandSize,
            CardsExiled = cardsToExile,
            CardsBottomed = cardsToBottom,
            ActivatedAt = DateTime.UtcNow
        };
        database.SerumPowderActivations.Add(activation);

        // Update session activation count
        session.TotalSerumPowderActivations++;

        // Get available cards (original deck minus exiled cards)
        var availableCards = decklist.Cards
            .Where(c => !exiledCards.Any(ec => CardsEqual(ec, c)))
            .ToArray();

        // Shuffle available cards and draw new hand
        var shuffledDeck = ShuffleDeck(availableCards);
        _activeDecks[session.Id] = shuffledDeck;

        // Draw the same number of cards as target hand size (not always 7!)
        var newHandCards = DrawHand(shuffledDeck, targetHandSize);

        // Generate NEW gamble odds for the Serum Powder hand
        var newGambleOdds = GenerateLowestGambleOdds();

        // Create new hand with same hand number (Serum Powder doesn't change hand number)
        var handId = handEvaluationService.GetNextHandId();
        var newHand = new Hand(
            handId,
            newHandCards,
            metadata.seatOrder,
            metadata.stickers,
            session.DecklistId,
            session.DecklistName,
            newGambleOdds
        );

        await database.SaveChangesAsync();

        // Return new hand with same hand number and target size
        return new FullMulliganEvaluationResponse(
            SessionComplete: false,
            NextHand: newHand,
            NextHandNumber: request.HandNumber, // Same hand number!
            NextTargetHandSize: targetHandSize, // Same target size!
            CanMulligan: targetHandSize > 3,
            Summary: null
        );
    }

    /// <summary>
    /// Compares two cards for equality based on name and type.
    /// </summary>
    private static bool CardsEqual(Card card1, Card card2)
    {
        return card1.Name.Equals(card2.Name, StringComparison.OrdinalIgnoreCase) &&
               card1.Type.Equals(card2.Type, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
