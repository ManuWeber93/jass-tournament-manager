using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JassTournamentManager.Infrastructure.Persistence.Repositories
{
    public sealed class TournamentRepository : ITournamentRepository
    {
        private readonly JtmDbContext _dbContext;

        public TournamentRepository(JtmDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Tournament tournament, CancellationToken cancellationToken)
        {
            await _dbContext.Tournaments.AddAsync(tournament, cancellationToken);
        }

        public Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _dbContext.Tournaments
                .Include(tournament => tournament.Rounds)
                .Include(tournament => tournament.Participants)
                .AsSplitQuery()
                .SingleOrDefaultAsync(tournament => tournament.Id == id, cancellationToken);
        }

        public Task<bool> ExistsByCodeAsync(string tournamentCode, CancellationToken cancellationToken)
        {
            return _dbContext.Tournaments.AnyAsync(
                tournament => tournament.Details.TournamentCode == tournamentCode,
                cancellationToken);
        }
    }
}