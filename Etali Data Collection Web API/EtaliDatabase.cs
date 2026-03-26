namespace Etali_Data_Collection_Web_API;

using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Models.Auth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class EtaliDatabase(DbContextOptions<EtaliDatabase> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<EvaluatedHand> EvaluatedHands { get; set; } = null!;
    public DbSet<MaximumManaEvaluatedHand> MaximumManaEvaluatedHands { get; set; } = null!;
    public DbSet<Decklist> Decklists { get; set; } = null!;
    public DbSet<DeckComparison> DeckComparisons { get; set; } = null!;
    public DbSet<ComparisonEvaluatedHand> ComparisonEvaluatedHands { get; set; } = null!;
    public DbSet<FullMulliganSession> FullMulliganSessions { get; set; } = null!;
    public DbSet<MulliganHandEvaluation> MulliganHandEvaluations { get; set; } = null!;
    public DbSet<SerumPowderActivation> SerumPowderActivations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use snake_case naming convention for PostgreSQL
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());
            
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToSnakeCase());
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(foreignKey.GetConstraintName()?.ToSnakeCase());
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }

        // Configure EvaluatedHand
        modelBuilder.Entity<EvaluatedHand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            // Store Cards as JSONB for efficient querying
            entity.Property(e => e.Cards)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Card[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Card>());

            // Store Stickers as JSONB
            entity.Property(e => e.Stickers)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<int[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<int>());

            entity.Property(e => e.DecklistName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.CreatedByUserId)
                .HasMaxLength(450);

            entity.Property(e => e.LowestGambleOdds)
                .HasColumnType("decimal(5,4)");

            // Indexes for performance
            entity.HasIndex(e => e.DecklistId)
                .HasDatabaseName("ix_evaluated_hands_decklist_id");
            
            entity.HasIndex(e => new { e.DecklistId, e.CanCastEtali })
                .HasDatabaseName("ix_evaluated_hands_decklist_can_cast");

            entity.HasIndex(e => e.CreatedByUserId)
                .HasDatabaseName("ix_evaluated_hands_created_by_user");

            // Foreign key to Decklist
            entity.HasOne<Decklist>()
                .WithMany()
                .HasForeignKey(e => e.DecklistId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to ApplicationUser
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Check constraints
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_evaluated_hands_min_cards", 
                    "minimum_cards_to_cast_etali >= 1 AND minimum_cards_to_cast_etali <= 8");
                t.HasCheckConstraint("ck_evaluated_hands_seat_order", 
                    "seat_order >= 1 AND seat_order <= 4");
            });

        // Configure MaximumManaEvaluatedHand
        modelBuilder.Entity<MaximumManaEvaluatedHand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.Cards)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Card[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Card>());

            entity.Property(e => e.Stickers)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<int[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<int>());

            entity.Property(e => e.DecklistName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.CreatedByUserId)
                .HasMaxLength(450);

            entity.Property(e => e.LowestGambleOdds)
                .HasColumnType("decimal(5,4)");

            entity.HasIndex(e => e.DecklistId)
                .HasDatabaseName("ix_maximum_mana_evaluated_hands_decklist_id");

            entity.HasIndex(e => e.CreatedByUserId)
                .HasDatabaseName("ix_maximum_mana_evaluated_hands_created_by_user");

            entity.HasOne<Decklist>()
                .WithMany()
                .HasForeignKey(e => e.DecklistId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_maximum_mana_evaluated_hands_seat_order",
                    "seat_order >= 1 AND seat_order <= 4");
                t.HasCheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_7",
                    "maximum_mana_in7_card_hand >= 0");
                t.HasCheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_6",
                    "maximum_mana_in6_card_hand >= 0");
                t.HasCheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_5",
                    "maximum_mana_in5_card_hand >= 0");
                t.HasCheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_4",
                    "maximum_mana_in4_card_hand >= 0");
                t.HasCheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_3",
                    "maximum_mana_in3_card_hand >= 0");
            });
        });
        });

        // Configure Decklist
        modelBuilder.Entity<Decklist>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).ValueGeneratedNever();

            entity.Property(d => d.Name)
                .HasMaxLength(500)
                .IsRequired();

            // Store Cards as JSONB for efficient querying
            entity.Property(d => d.Cards)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Card[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Card>());

            entity.Property(d => d.MoxfieldUrl)
                .HasMaxLength(1000);

            entity.Property(d => d.MoxfieldId)
                .HasMaxLength(100);

            entity.Property(d => d.CreatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(d => d.CreatedByUserId)
                .HasMaxLength(450);

            // Indexes for performance
            entity.HasIndex(d => d.Name)
                .HasDatabaseName("ix_decklists_name");

            entity.HasIndex(d => d.MoxfieldId)
                .HasDatabaseName("ix_decklists_moxfield_id");

            entity.HasIndex(d => d.CreatedAt)
                .HasDatabaseName("ix_decklists_created_at");

            entity.HasIndex(d => d.CreatedByUserId)
                .HasDatabaseName("ix_decklists_created_by_user");

            // Foreign key to ApplicationUser
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure DeckComparison
        modelBuilder.Entity<DeckComparison>(entity =>
        {
            entity.HasKey(dc => dc.Id);
            entity.Property(dc => dc.Id).ValueGeneratedNever();

            entity.Property(dc => dc.Deck1Name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(dc => dc.Deck2Name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(dc => dc.CreatedAt)
                .HasColumnType("timestamp with time zone");

            // Indexes for performance
            entity.HasIndex(dc => dc.Deck1Id)
                .HasDatabaseName("ix_deck_comparisons_deck1_id");

            entity.HasIndex(dc => dc.Deck2Id)
                .HasDatabaseName("ix_deck_comparisons_deck2_id");

            entity.HasIndex(dc => new { dc.Deck1Id, dc.Deck2Id })
                .IsUnique()
                .HasDatabaseName("ix_deck_comparisons_deck_ids_unique");

            entity.HasIndex(dc => dc.CreatedAt)
                .HasDatabaseName("ix_deck_comparisons_created_at");

            // Foreign keys to Decklists (named explicitly to avoid conflicts)
            entity.HasOne<Decklist>()
                .WithMany()
                .HasForeignKey(dc => dc.Deck1Id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_deck_comparisons_decklists_deck1_id");

            entity.HasOne<Decklist>()
                .WithMany()
                .HasForeignKey(dc => dc.Deck2Id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_deck_comparisons_decklists_deck2_id");

            // Check constraint to ensure deck1Id < deck2Id (canonical ordering)
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_deck_comparisons_deck_order", 
                    "deck1_id < deck2_id");
            });
        });

        // Configure ComparisonEvaluatedHand
        modelBuilder.Entity<ComparisonEvaluatedHand>(entity =>
        {
            entity.HasKey(ceh => ceh.Id);
            entity.Property(ceh => ceh.Id).ValueGeneratedNever();

            // Store Cards as JSONB
            entity.Property(ceh => ceh.Cards)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Card[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Card>());

            // Store Stickers as JSONB
            entity.Property(ceh => ceh.Stickers)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<int[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<int>());

            entity.Property(ceh => ceh.DecklistName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(ceh => ceh.CreatedByUserId)
                .HasMaxLength(450);

            entity.Property(ceh => ceh.LowestGambleOdds)
                .HasColumnType("decimal(5,4)");

            // Indexes for performance
            entity.HasIndex(ceh => ceh.ComparisonId)
                .HasDatabaseName("ix_comparison_evaluated_hands_comparison_id");

            entity.HasIndex(ceh => new { ceh.ComparisonId, ceh.WhichDeck })
                .HasDatabaseName("ix_comparison_evaluated_hands_comparison_deck");

            entity.HasIndex(ceh => ceh.CreatedByUserId)
                .HasDatabaseName("ix_comparison_evaluated_hands_created_by_user");

            // Foreign key to DeckComparison
            entity.HasOne<DeckComparison>()
                .WithMany()
                .HasForeignKey(ceh => ceh.ComparisonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to ApplicationUser
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ceh => ceh.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Check constraints
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_comparison_evaluated_hands_which_deck", 
                    "which_deck >= 1 AND which_deck <= 2");
                t.HasCheckConstraint("ck_comparison_evaluated_hands_min_cards", 
                    "minimum_cards_to_cast_etali >= 1 AND minimum_cards_to_cast_etali <= 8");
                t.HasCheckConstraint("ck_comparison_evaluated_hands_seat_order", 
                    "seat_order >= 1 AND seat_order <= 4");
                t.HasCheckConstraint("ck_comparison_evaluated_hands_gen_attempts", 
                    "generation_attempts >= 1");
            });
        });

        // Configure FullMulliganSession
        modelBuilder.Entity<FullMulliganSession>(entity =>
        {
            entity.HasKey(fms => fms.Id);

            entity.Property(fms => fms.DecklistName)
                .HasMaxLength(500)
                .IsRequired();

            // Store Stickers as JSONB
            entity.Property(fms => fms.Stickers)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<int[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<int>());

            entity.Property(fms => fms.CreatedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(fms => fms.CreatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(fms => fms.CompletedAt)
                .HasColumnType("timestamp with time zone");

            // Indexes for performance
            entity.HasIndex(fms => fms.DecklistId)
                .HasDatabaseName("ix_full_mulligan_sessions_decklist_id");

            entity.HasIndex(fms => fms.CreatedByUserId)
                .HasDatabaseName("ix_full_mulligan_sessions_created_by_user");

            entity.HasIndex(fms => new { fms.DecklistId, fms.IsActive })
                .HasDatabaseName("ix_full_mulligan_sessions_decklist_active");

            entity.HasIndex(fms => fms.CreatedAt)
                .HasDatabaseName("ix_full_mulligan_sessions_created_at");

            // Foreign key to Decklist
            entity.HasOne<Decklist>()
                .WithMany()
                .HasForeignKey(fms => fms.DecklistId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to ApplicationUser
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(fms => fms.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check constraints
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_full_mulligan_sessions_seat_order",
                    "seat_order >= 1 AND seat_order <= 4");
                t.HasCheckConstraint("ck_full_mulligan_sessions_final_hand_size",
                    "final_hand_size >= 0 AND final_hand_size <= 7");
                t.HasCheckConstraint("ck_full_mulligan_sessions_total_mulligans",
                    "total_mulligans >= 0 AND total_mulligans <= 5");
            });
        });

        // Configure MulliganHandEvaluation
        modelBuilder.Entity<MulliganHandEvaluation>(entity =>
        {
            entity.HasKey(mhe => mhe.Id);

            // Store Cards as JSONB
            entity.Property(mhe => mhe.Cards)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Card[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Card>());

            entity.Property(mhe => mhe.EvaluatedAt)
                .HasColumnType("timestamp with time zone");

            // Indexes for performance
            entity.HasIndex(mhe => mhe.SessionId)
                .HasDatabaseName("ix_mulligan_hand_evaluations_session_id");

            entity.HasIndex(mhe => new { mhe.SessionId, mhe.HandNumber })
                .IsUnique()
                .HasDatabaseName("ix_mulligan_hand_evaluations_session_hand_unique");

            // Foreign key to FullMulliganSession
            entity.HasOne<FullMulliganSession>()
                .WithMany()
                .HasForeignKey(mhe => mhe.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Check constraints
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_mulligan_hand_evaluations_hand_number",
                    "hand_number >= 1 AND hand_number <= 6");
                t.HasCheckConstraint("ck_mulligan_hand_evaluations_target_hand_size",
                    "target_hand_size IN (3, 4, 5, 6, 7)");
            });
        });

        // Configure SerumPowderActivation
        modelBuilder.Entity<SerumPowderActivation>(entity =>
        {
            entity.HasKey(spa => spa.Id);

            // Store CardsExiled as JSONB
            entity.Property(spa => spa.CardsExiled)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Card[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Card>());

            // Store CardsBottomed as JSONB
            entity.Property(spa => spa.CardsBottomed)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Card[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Card>());

            entity.Property(spa => spa.ActivatedAt)
                .HasColumnType("timestamp with time zone");

            // Indexes for performance
            entity.HasIndex(spa => spa.SessionId)
                .HasDatabaseName("ix_serum_powder_activations_session_id");

            entity.HasIndex(spa => new { spa.SessionId, spa.HandNumber })
                .HasDatabaseName("ix_serum_powder_activations_session_hand");

            // Foreign key to FullMulliganSession
            entity.HasOne<FullMulliganSession>()
                .WithMany()
                .HasForeignKey(spa => spa.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Check constraints
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_serum_powder_activations_hand_number",
                    "hand_number >= 1 AND hand_number <= 6");
                t.HasCheckConstraint("ck_serum_powder_activations_target_hand_size",
                    "target_hand_size IN (3, 4, 5, 6, 7)");
            });
        });

        // Configure Identity tables to use snake_case
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.CreatedAt)
                .HasColumnType("timestamp with time zone");
        });
    }
}

/// <summary>
/// Extension method to convert strings to snake_case for PostgreSQL naming convention.
/// </summary>
public static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
