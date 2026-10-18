using JassTournamentManager.Contracts.Auth;

namespace JassTournamentManager.App.Infrastructure.Auth
{
    public interface IAuthApiClient
    {
        Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        Task<bool> LogoutAsync(StoredAuthTokens tokens, CancellationToken cancellationToken = default);
    }
}