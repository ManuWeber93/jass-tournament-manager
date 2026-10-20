using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Domain.Entities;

namespace JassTournamentManager.Application.Tests.Tournaments
{
    internal sealed class FakeTournamentRepository : ITournamentRepository
    {
        public List<Tournament> Tournaments { get; } = [];
        public List<string> CheckedCodes { get; } = [];
        public HashSet<string> OccupiedCodes { get; } = [];
        public List<CancellationToken> ReceivedTokens { get; } = [];
        public int AddCallCount { get; private set; }
        public int GetCallCount { get; private set; }
        public Exception? Exception { get; set; }

        public Task AddAsync(Tournament tournament, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            AddCallCount++;
            Tournaments.Add(tournament);
            return Task.CompletedTask;
        }

        public Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            GetCallCount++;
            return Task.FromResult(Tournaments.SingleOrDefault(tournament => tournament.Id == id));
        }

        public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            CheckedCodes.Add(code);
            return Task.FromResult(OccupiedCodes.Contains(code));
        }

        private void Observe(CancellationToken cancellationToken)
        {
            ReceivedTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (Exception is not null)
            {
                throw Exception;
            }
        }
    }
}