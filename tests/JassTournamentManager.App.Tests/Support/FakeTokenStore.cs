using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.Contracts.Auth;

namespace JassTournamentManager.App.Tests.Support;

internal sealed class FakeTokenStore : ITokenStore
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public AuthResponse? SavedResponse { get; private set; }
    public List<(string Operation, CancellationToken Token)> Calls { get; } = new();

    private void Record(string operation, CancellationToken token)
    {
        Calls.Add((operation, token));
        token.ThrowIfCancellationRequested();
    }

    public Task<StoredAuthTokens?> GetTokensAsync(CancellationToken cancellationToken = default)
    {
        Record(nameof(GetTokensAsync), cancellationToken);
        return Task.FromResult<StoredAuthTokens?>(null);
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        Record(nameof(GetAccessTokenAsync), cancellationToken);
        return Task.FromResult(AccessToken);
    }

    public Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        Record(nameof(GetRefreshTokenAsync), cancellationToken);
        return Task.FromResult(RefreshToken);
    }

    public Task SetTokensAsync(AuthResponse authResponse, CancellationToken cancellationToken = default)
    {
        Record(nameof(SetTokensAsync), cancellationToken);
        SavedResponse = authResponse;
        AccessToken = authResponse.AccessToken;
        RefreshToken = authResponse.RefreshToken;
        return Task.CompletedTask;
    }

    public Task SetTokensAsync(StoredAuthTokens tokens, CancellationToken cancellationToken = default)
    {
        Record(nameof(SetTokensAsync), cancellationToken);
        AccessToken = tokens.AccessToken;
        RefreshToken = tokens.RefreshToken;
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Record(nameof(ClearAsync), cancellationToken);
        AccessToken = null;
        RefreshToken = null;
        return Task.CompletedTask;
    }
}