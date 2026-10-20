using JassTournamentManager.Application.Common;
using JassTournamentManager.Contracts.Tournaments;

namespace JassTournamentManager.Application.Tournaments
{
    public interface ITournamentService
    {
        Task<Result<TournamentResponse>> CreateAsync(CreateTournamentRequest request, CancellationToken cancellationToken);
        Task<Result<TournamentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}