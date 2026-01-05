using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Etali.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that properly configures the test environment.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly object _lock = new object();
    private static bool _seeded = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll<DbContextOptions<EtaliDatabase>>();

            // Add a database context (using in-memory for tests)
            services.AddDbContext<EtaliDatabase>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Build service provider and seed data
            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();

            // Ensure database is created
            db.Database.EnsureCreated();

            // Thread-safe seeding - only seed once
            lock (_lock)
            {
                if (!_seeded && !db.Decklists.Any())
                {
                    // Add first test deck
                    db.Decklists.Add(new Decklist
                    {
                        Id = 1,
                        Name = "TestDeck",
                        Cards = new[]
                        {
                            new Card { Name = "Sol Ring", Type = "Artifact" },
                            new Card { Name = "Mana Vault", Type = "Artifact" },
                            new Card { Name = "Card3", Type = "Sorcery" },
                            new Card { Name = "Card4", Type = "Artifact" },
                            new Card { Name = "Card5", Type = "Enchantment" },
                            new Card { Name = "Mountain", Type = "Land" },
                            new Card { Name = "Command Tower", Type = "Land" },
                            new Card { Name = "Card8", Type = "Creature" },
                            new Card { Name = "Card9", Type = "Instant" },
                            new Card { Name = "Card10", Type = "Land" }
                        },
                        MoxfieldUrl = "https://www.moxfield.com/decks/test123",
                        MoxfieldId = "test123",
                        CreatedAt = DateTime.UtcNow
                    });
                    
                    // Add second test deck for comparisons (with some unique and some shared cards)
                    db.Decklists.Add(new Decklist
                    {
                        Id = 2,
                        Name = "TestDeck2",
                        Cards = new[]
                        {
                            new Card { Name = "Worn Powerstone", Type = "Artifact" },
                            new Card { Name = "Thran Dynamo", Type = "Artifact" },
                            new Card { Name = "Card3", Type = "Sorcery" },
                            new Card { Name = "Card4", Type = "Artifact" },
                            new Card { Name = "Card5", Type = "Enchantment" },
                            new Card { Name = "Mountain", Type = "Land" },
                            new Card { Name = "Command Tower", Type = "Land" },
                            new Card { Name = "Card11", Type = "Creature" },
                            new Card { Name = "Card12", Type = "Instant" },
                            new Card { Name = "Card13", Type = "Land" }
                        },
                        MoxfieldUrl = "https://www.moxfield.com/decks/test456",
                        MoxfieldId = "test456",
                        CreatedAt = DateTime.UtcNow
                    });
                    
                    db.SaveChanges();
                    _seeded = true;
                }
            }
        });
    }

    /// <summary>
    /// Helper method to create test Card arrays for tests that need to create EvaluatedHand objects.
    /// </summary>
    public static Card[] CreateTestCards(params string[] names)
    {
        return names.Select(name => new Card { Name = name, Type = "Creature" }).ToArray();
    }
}
