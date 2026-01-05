namespace Etali_Data_Collection_Web_API;

using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class EtaliDatabase(DbContextOptions<EtaliDatabase> options) : DbContext(options)
{
    public DbSet<EvaluatedHand> EvaluatedHands { get; set; } = null!;
    public DbSet<Decklist> Decklists { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure EvaluatedHand
        modelBuilder.Entity<EvaluatedHand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Cards)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Card[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Card>());
            entity.Property(e => e.Stickers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<int[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<int>());
        });

        // Configure Decklist
        modelBuilder.Entity<Decklist>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Cards)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Card[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Card>());
        });
    }
}
