using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Contracts.Tournaments;

namespace JassTournamentManager.Api.Tests.Tournaments
{
    internal sealed class FakeTournamentService : ITournamentService
    {
        public Result<TournamentResponse>? CreateAsyncResult { get; set; }
        public Result<TournamentResponse>? GetByIdAsyncResult { get; set; }
        public CreateTournamentRequest? ReceivedCreateAsyncRequest { get; private set; }
        public Guid? ReceivedGetByIdAsyncRequest { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }
        public int CreateAsyncCallCount { get; private set; }
        public int GetByIdAsyncCallCount { get; private set; }

        public Task<Result<TournamentResponse>> CreateAsync(CreateTournamentRequest request, CancellationToken cancellationToken)
        {
            ReceivedCreateAsyncRequest = request;
            ReceivedCancellationToken = cancellationToken;
            CreateAsyncCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateAsyncResult!);
        }

        public Task<Result<TournamentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            ReceivedGetByIdAsyncRequest = id;
            ReceivedCancellationToken = cancellationToken;
            GetByIdAsyncCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetByIdAsyncResult!);
        }
    }
}