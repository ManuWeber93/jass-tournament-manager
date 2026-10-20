using JassTournamentManager.Application.Common;

namespace JassTournamentManager.Application.Tests.Tournaments
{
    internal sealed class FakeTournamentUnitOfWork : IUnitOfWork
    {
        public List<CancellationToken> ReceivedTokens { get; } = [];
        public Action<int>? OnSave { get; set; }
        public int SaveChangesCallCount => ReceivedTokens.Count;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            ReceivedTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            OnSave?.Invoke(SaveChangesCallCount);
            return Task.FromResult(1);
        }
    }
}