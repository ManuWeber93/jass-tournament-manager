using JassTournamentManager.Contracts.Auth;

namespace JassTournamentManager.App.Infrastructure.Auth;

public interface ITokenStore
{
    Task<StoredAuthTokens?> GetTokensAsync(CancellationToken cancellationToken = default);

    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default);

    Task SetTokensAsync(AuthResponse authResponse, CancellationToken cancellationToken = default);

    Task SetTokensAsync(StoredAuthTokens tokens, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}