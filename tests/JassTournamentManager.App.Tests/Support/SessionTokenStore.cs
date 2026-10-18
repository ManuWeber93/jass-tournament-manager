using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.Contracts.Auth;

namespace JassTournamentManager.App.Tests.Support;

internal sealed class SessionTokenStore : ITokenStore
{
    public StoredAuthTokens? Tokens { get; set; }
    public Func<CancellationToken, Task>? WriteHandler { get; set; }
    public Exception? ClearException { get; set; }
    public List<CancellationToken> ClearCalls { get; } = new();

    public Task<StoredAuthTokens?> GetTokensAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Tokens);
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult(Tokens?.AccessToken);
    public Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult(Tokens?.RefreshToken);

    public Task SetTokensAsync(AuthResponse response, CancellationToken cancellationToken = default) =>
        SetTokensAsync(new StoredAuthTokens(response.AccessToken, response.AccessTokenExpiresAt,
            response.RefreshToken, response.RefreshTokenExpiresAt), cancellationToken);

    public async Task SetTokensAsync(StoredAuthTokens tokens, CancellationToken cancellationToken = default)
    {
        Tokens = tokens;
        if (WriteHandler is not null)
        {
            await WriteHandler(cancellationToken);
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ClearCalls.Add(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (ClearException is not null)
        {
            throw ClearException;
        }
        Tokens = null;
        return Task.CompletedTask;
    }
}