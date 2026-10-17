using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.TournamentTemplates;
using JassTournamentManager.Contracts.TournamentTemplates;

namespace JassTournamentManager.Api.Tests.TournamentTemplates
{
    internal sealed class FakeTournamentTemplateService : ITournamentTemplateService
    {
        public Result<TournamentTemplateResponse>? CreateAsyncResult { get; set; }

        public Result<TournamentTemplateResponse>? GetByIdAsyncResult { get; set; }

        public Result<TournamentTemplateResponse>? GetForCurrentUserAsyncResult { get; set; }

        public CreateTournamentTemplateRequest? ReceivedCreateAsyncRequest { get; private set; }

        public Guid? ReceivedGetByIdAsyncRequest { get; private set; }

        public int CreateAsyncCallCount { get; private set; }

        public int GetForCurrentUserAsyncCallCount { get; private set; }

        public Task<Result<TournamentTemplateResponse>> CreateAsync(CreateTournamentTemplateRequest request, CancellationToken cancellationToken)
        {
            ReceivedCreateAsyncRequest = request;
            CreateAsyncCallCount++;
            return Task.FromResult(CreateAsyncResult!);
        }

        public Task<Result<TournamentTemplateResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            ReceivedGetByIdAsyncRequest = id;
            return Task.FromResult(GetByIdAsyncResult!);
        }

        public Task<Result<TournamentTemplateResponse>> GetForCurrentUserAsync(CancellationToken cancellationToken)
        {
            GetForCurrentUserAsyncCallCount++;
            return Task.FromResult(GetForCurrentUserAsyncResult!);
        }
    }
}

