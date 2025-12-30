using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API
{
    public class EtaliDatabase : DbContext
    {
        public EtaliDatabase(DbContextOptions<EtaliDatabase> options) : base(options) { }
        
        public DbSet<EvaluatedHand> EvaluatedHands { get; set; } = null!;
        public DbSet<Decklist> Decklists { get; set; } = null!;
    }
}
