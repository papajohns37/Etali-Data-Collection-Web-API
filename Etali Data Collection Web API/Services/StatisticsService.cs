using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service for calculating statistical analysis of decklist hand testing data.
/// </summary>
public class StatisticsService(EtaliDatabase database) : IStatisticsService
{
	private const double ZScore90Percent = 1.645; // Z-score for 90% confidence interval
	private static readonly int[] HandSizes = [7, 6, 5, 4, 3];

	/// <inheritdoc/>
	public async Task<DecklistStatistics?> GetDecklistStatistics(int decklistId)
	{
		// Verify decklist exists
		var decklist = await database.Decklists.FirstOrDefaultAsync(d => d.Id == decklistId);
		if (decklist == null)
		{
			return null;
		}

		// Get all evaluated hands for this decklist
		var hands = await database.EvaluatedHands
			.Where(h => h.DecklistId == decklistId)
			.ToListAsync();

		int totalHandsTested = hands.Count;

		// If no hands tested, return empty statistics
		if (totalHandsTested == 0)
		{
			return new DecklistStatistics(
				DecklistId: decklistId,
				DecklistName: decklist.Name,
				TotalHandsTested: 0,
				HandSizeStatistics: [],
				OverallMulliganProbability: 0,
				OverallMulliganConfidenceIntervalLower: 0,
				OverallMulliganConfidenceIntervalUpper: 0
			);
		}

		// Calculate statistics for each hand size (7, 6, 5, 4, 3)
		var handSizeStats = new List<HandSizeStatistics>();
		var probabilities = new Dictionary<int, (double probability, double ciLower, double ciUpper)>();

		foreach (int handSize in HandSizes)
		{
			var stats = CalculateHandSizeStatistics(hands, handSize);
			handSizeStats.Add(stats);
			probabilities[handSize] = (stats.Probability, stats.ConfidenceIntervalLower, stats.ConfidenceIntervalUpper);
		}

		// Calculate overall mulligan probability
		// Formula: 1 - (1 - p7)^2 * (1 - p6) * (1 - p5) * (1 - p4) * (1 - p3)
		var (overallProb, overallCILower, overallCIUpper) = CalculateOverallMulliganProbability(probabilities, hands);

		return new DecklistStatistics(
			DecklistId: decklistId,
			DecklistName: decklist.Name,
			TotalHandsTested: totalHandsTested,
			HandSizeStatistics: [.. handSizeStats],
			OverallMulliganProbability: overallProb,
			OverallMulliganConfidenceIntervalLower: overallCILower,
			OverallMulliganConfidenceIntervalUpper: overallCIUpper
		);
	}

	/// <summary>
	/// Calculates statistics for a specific hand size.
	/// </summary>
	/// <param name="hands">All evaluated hands for the decklist.</param>
	/// <param name="handSize">The hand size to calculate statistics for (3-7).</param>
	/// <returns>Statistics for the specified hand size.</returns>
	private static HandSizeStatistics CalculateHandSizeStatistics(List<EvaluatedHand> hands, int handSize)
	{
		// A hand is successful at size N if minimumCardsToCastEtali <= N
		// This means the hand can cast Etali with N or fewer cards
		int totalHands = hands.Count;
		int successfulHands = hands.Count(h => h.CanCastEtali && h.MinimumCardsToCastEtali <= handSize);

		double probability = totalHands > 0 ? (double)successfulHands / totalHands : 0;

		// Calculate Wilson score confidence interval
		var (ciLower, ciUpper) = CalculateWilsonScoreInterval(successfulHands, totalHands, ZScore90Percent);

		return new HandSizeStatistics(
			HandSize: handSize,
			TotalHands: totalHands,
			SuccessfulHands: successfulHands,
			Probability: probability,
			ConfidenceIntervalLower: ciLower,
			ConfidenceIntervalUpper: ciUpper
		);
	}

	/// <summary>
	/// Calculates the Wilson score confidence interval for a binomial proportion.
	/// </summary>
	/// <param name="successes">Number of successes.</param>
	/// <param name="total">Total number of trials.</param>
	/// <param name="zScore">Z-score for desired confidence level (1.645 for 90%).</param>
	/// <returns>Tuple of (lower bound, upper bound) for the confidence interval.</returns>
	/// <remarks>
	/// The Wilson score interval is more accurate than the normal approximation,
	/// especially for small sample sizes or extreme probabilities (near 0 or 1).
	/// Formula: https://en.wikipedia.org/wiki/Binomial_proportion_confidence_interval#Wilson_score_interval
	/// </remarks>
	private static (double lower, double upper) CalculateWilsonScoreInterval(int successes, int total, double zScore)
	{
		if (total == 0)
		{
			return (0, 0);
		}

		double n = total;
		double p = successes / n;
		double z2 = zScore * zScore;

		// Wilson score interval formula
		double denominator = 1 + z2 / n;
		double centerAdjustment = p + z2 / (2 * n);
		double marginOfError = zScore * Math.Sqrt((p * (1 - p) + z2 / (4 * n)) / n);

		double lower = (centerAdjustment - marginOfError) / denominator;
		double upper = (centerAdjustment + marginOfError) / denominator;

		// Clamp to [0, 1] range
		lower = Math.Max(0, Math.Min(1, lower));
		upper = Math.Max(0, Math.Min(1, upper));

		return (lower, upper);
	}

	/// <summary>
	/// Calculates the overall mulligan probability and its confidence interval.
	/// </summary>
	/// <param name="probabilities">Dictionary mapping hand size to (probability, CI lower, CI upper).</param>
	/// <param name="hands">List of evaluated hands.</param>
	/// <returns>Tuple of (overall probability, CI lower, CI upper).</returns>
	/// <remarks>
	/// Formula: 1 - (1 - p7)^2 * (1 - p6) * (1 - p5) * (1 - p4) * (1 - p3)
	/// This represents seeing two 7-card hands, then one each of 6, 5, 4, and 3 card hands.
	/// 
	/// Confidence interval uses bootstrap method with parallel processing for better accuracy.
	/// </remarks>
	private (double probability, double ciLower, double ciUpper) CalculateOverallMulliganProbability(
		Dictionary<int, (double probability, double ciLower, double ciUpper)> probabilities, List<EvaluatedHand> hands)
	{
		// Extract probabilities
		var p7 = probabilities.TryGetValue(7, out var p7Data) ? p7Data.probability : 0;
		var p6 = probabilities.TryGetValue(6, out var p6Data) ? p6Data.probability : 0;
		var p5 = probabilities.TryGetValue(5, out var p5Data) ? p5Data.probability : 0;
		var p4 = probabilities.TryGetValue(4, out var p4Data) ? p4Data.probability : 0;
		var p3 = probabilities.TryGetValue(3, out var p3Data) ? p3Data.probability : 0;

		// Calculate overall probability
		double overallProb = 1 - Math.Pow(1 - p7, 2) * (1 - p6) * (1 - p5) * (1 - p4) * (1 - p3);

		// Bootstrap method for better CI estimation of the mulligan probability
		int bootstrapSamples = 10_000;
		double[] overallProbabilities = new double[bootstrapSamples];
		int handsCount = hands.Count;

		// Use Parallel.For for concurrent processing of bootstrap samples
		Parallel.For(0, bootstrapSamples, () => Random.Shared, (sampleIndex, loopState, localRandom) =>
		{
			int sum7 = 0;
			int sum6 = 0;
			int sum5 = 0;
			int sum4 = 0;
			int sum3 = 0;

			// Resample with replacement
			for (int handNumber = 0; handNumber < handsCount; handNumber++)
			{
				EvaluatedHand randomHand = hands[localRandom.Next(handsCount)];

				if (randomHand.CanCastEtali)
				{
					int minCards = randomHand.MinimumCardsToCastEtali;

					// Optimized: single pass through cascading ifs
					if (minCards <= 3)
					{
						sum3++;
						sum4++;
						sum5++;
						sum6++;
						sum7++;
					}
					else if (minCards <= 4)
					{
						sum4++;
						sum5++;
						sum6++;
						sum7++;
					}
					else if (minCards <= 5)
					{
						sum5++;
						sum6++;
						sum7++;
					}
					else if (minCards <= 6)
					{
						sum6++;
						sum7++;
					}
					else if (minCards <= 7)
					{
						sum7++;
					}
				}
			}

			// Calculate probabilities for this bootstrap sample
			double invHandsCount = 1.0 / handsCount;
			double p7Sample = sum7 * invHandsCount;
			double p6Sample = sum6 * invHandsCount;
			double p5Sample = sum5 * invHandsCount;
			double p4Sample = sum4 * invHandsCount;
			double p3Sample = sum3 * invHandsCount;

			// Calculate overall probability for this sample
			overallProbabilities[sampleIndex] = 1 - Math.Pow(1 - p7Sample, 2) * (1 - p6Sample) * (1 - p5Sample) * (1 - p4Sample) * (1 - p3Sample);

			return localRandom;
		},
		_ => { });

		// Find the 5th and 95th percentiles using QuickSelect (much faster than full sort)
		// We only need two values, not the entire sorted array
		int lowerIndex = (int)(0.05 * bootstrapSamples);
		int upperIndex = (int)(0.95 * bootstrapSamples);

		// Use QuickSelect for O(n) average case vs O(n log n) for full sort
		double bootstrapLower = QuickSelect(overallProbabilities, lowerIndex);
		double bootstrapUpper = QuickSelect(overallProbabilities, upperIndex);

		return (overallProb, bootstrapLower, bootstrapUpper);
	}

	/// <summary>
	/// Finds the k-th smallest element using QuickSelect algorithm (average O(n)).
	/// Much faster than sorting when only a specific percentile is needed.
	/// </summary>
	private static double QuickSelect(double[] arr, int k)
	{
		int left = 0;
		int right = arr.Length - 1;
		Random rnd = Random.Shared;

		while (left < right)
		{
			// Partition around a random pivot
			int pivotIndex = left + rnd.Next(right - left + 1);
			double pivot = arr[pivotIndex];

			// Move pivot to end
			(arr[pivotIndex], arr[right]) = (arr[right], arr[pivotIndex]);

			int storeIndex = left;
			for (int i = left; i < right; i++)
			{
				if (arr[i] < pivot)
				{
					(arr[i], arr[storeIndex]) = (arr[storeIndex], arr[i]);
					storeIndex++;
				}
			}

			// Move pivot to final position
			(arr[storeIndex], arr[right]) = (arr[right], arr[storeIndex]);

			if (storeIndex == k)
			{
				return arr[k];
			}
			else if (k < storeIndex)
			{
				right = storeIndex - 1;
			}
			else
			{
				left = storeIndex + 1;
			}
		}

		return arr[k];
	}
}
