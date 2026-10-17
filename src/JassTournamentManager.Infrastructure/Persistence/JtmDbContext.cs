using JassTournamentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JassTournamentManager.Infrastructure.Persistence
{
    public sealed class JtmDbContext : DbContext
    {
        public JtmDbContext(DbContextOptions<JtmDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<TournamentTemplate> TournamentTemplates => Set<TournamentTemplate>();

        public DbSet<JassTable> JassTables => Set<JassTable>();

        public DbSet<Tournament> Tournaments => Set<Tournament>();

        public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();

        public DbSet<Round> Rounds => Set<Round>();

        public DbSet<Pairing> Pairings => Set<Pairing>();

        public DbSet<PairingParticipant> PairingParticipants => Set<PairingParticipant>();

        public DbSet<Game> Games => Set<Game>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("jtm");

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(JtmDbContext).Assembly);
        }
    }
}
