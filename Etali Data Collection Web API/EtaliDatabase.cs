using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API
{
    public class EtaliDatabase : DbContext
    {
        public EtaliDatabase(DbContextOptions<EtaliDatabase> options) : base(options) { }
        
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
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.None));
                entity.Property(e => e.Stickers)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.None).Select(int.Parse).ToArray());
            });

            // Configure Decklist
            modelBuilder.Entity<Decklist>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.CardNames)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.None));
            });
        }
    }
}
