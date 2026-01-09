using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service for migrating old hand evaluation data from CSV files into the database.
/// </summary>
public interface ICsvMigrationService
{
    /// <summary>
    /// Import old hand evaluation data from a CSV file.
    /// </summary>
    /// <param name="csvFilePath">Path to the CSV file containing old hand data.</param>
    /// <returns>Number of records imported.</returns>
    Task<int> ImportOldHandDataAsync(string csvFilePath);
}

/// <summary>
/// Implementation of CSV migration service.
/// </summary>
public class CsvMigrationService : ICsvMigrationService
{
    private readonly EtaliDatabase _db;
    private readonly ILogger<CsvMigrationService> _logger;

    public CsvMigrationService(EtaliDatabase db, ILogger<CsvMigrationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> ImportOldHandDataAsync(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException($"CSV file not found at path: {csvFilePath}");
        }

        _logger.LogInformation("Starting import of old hand data from: {FilePath}", csvFilePath);

        var records = new List<OldEvaluatedHand>();
        var decklistCache = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);

        using (var reader = new StreamReader(csvFilePath))
        {
            // Skip header line
            var header = await reader.ReadLineAsync();
            if (header == null)
            {
                throw new InvalidOperationException("CSV file is empty");
            }

            _logger.LogInformation("CSV Header: {Header}", header);

            int lineNumber = 1;
            while (!reader.EndOfStream)
            {
                lineNumber++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var record = ParseCsvLine(line, lineNumber);
                    
                    // Try to match decklist by name
                    if (!decklistCache.ContainsKey(record.DecklistName))
                    {
                        var decklist = await _db.Decklists
                            .FirstOrDefaultAsync(d => d.Name.ToLower() == record.DecklistName.ToLower());
                        decklistCache[record.DecklistName] = decklist?.Id;
                    }
                    
                    record.DecklistId = decklistCache[record.DecklistName];
                    records.Add(record);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse line {LineNumber}: {Line}", lineNumber, line);
                }
            }
        }

        _logger.LogInformation("Parsed {Count} records from CSV", records.Count);

        // Import records in batches
        const int batchSize = 100;
        int totalImported = 0;

        for (int i = 0; i < records.Count; i += batchSize)
        {
            var batch = records.Skip(i).Take(batchSize).ToList();
            await _db.OldEvaluatedHands.AddRangeAsync(batch);
            await _db.SaveChangesAsync();
            totalImported += batch.Count;
            _logger.LogInformation("Imported {Current}/{Total} records", totalImported, records.Count);
        }

        _logger.LogInformation("Successfully imported {Count} old hand evaluation records", totalImported);
        return totalImported;
    }

    private OldEvaluatedHand ParseCsvLine(string line, int lineNumber)
    {
        // Parse CSV line with proper handling of quoted fields
        var fields = ParseCsvFields(line);

        if (fields.Count < 11)
        {
            throw new InvalidOperationException(
                $"Expected at least 11 fields, but got {fields.Count}. Line: {line}");
        }

        // CSV format: Decklist Name,User,Can Cast Etali,Minimum Cards,Card 1,Card 2,Card 3,Card 4,Card 5,Card 6,Card 7
        var record = new OldEvaluatedHand
        {
            DecklistName = fields[0],
            User = fields[1],
            CanCastEtali = ParseBoolean(fields[2]),
            MinimumCardsToCastEtali = int.Parse(fields[3], CultureInfo.InvariantCulture),
            Card1 = string.IsNullOrWhiteSpace(fields[4]) ? null : fields[4],
            Card2 = string.IsNullOrWhiteSpace(fields[5]) ? null : fields[5],
            Card3 = string.IsNullOrWhiteSpace(fields[6]) ? null : fields[6],
            Card4 = string.IsNullOrWhiteSpace(fields[7]) ? null : fields[7],
            Card5 = string.IsNullOrWhiteSpace(fields[8]) ? null : fields[8],
            Card6 = string.IsNullOrWhiteSpace(fields[9]) ? null : fields[9],
            Card7 = string.IsNullOrWhiteSpace(fields[10]) ? null : fields[10],
            ImportedAt = DateTime.UtcNow
        };

        return record;
    }

    private List<string> ParseCsvFields(string line)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString().Trim());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // Add the last field
        fields.Add(currentField.ToString().Trim());

        return fields;
    }

    private bool ParseBoolean(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim().ToLowerInvariant();
        
        return value switch
        {
            "true" or "yes" or "y" or "1" => true,
            "false" or "no" or "n" or "0" => false,
            _ => throw new InvalidOperationException($"Cannot parse boolean value: {value}")
        };
    }
}
