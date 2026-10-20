using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.Tournaments;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace JassTournamentManager.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly JtmDbContext _dbContext;

        public UnitOfWork(JtmDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "ux_tournaments_tournament_code"
                })
            {
                throw new TournamentCodeConflictException(exception);
            }
        }
    }
}
