using JassTournamentManager.Domain.Entities;

namespace JassTournamentManager.Application.Tournaments
{
    public interface ITournamentRepository
    {
        Task AddAsync(Tournament tournament, CancellationToken cancellationToken);
        Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);
    }
}