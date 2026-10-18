using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.Contracts.Auth;

namespace JassTournamentManager.App.Tests.Support;

internal sealed class FakeAuthApiClient : IAuthApiClient
{
    public List<(LoginRequest Request, CancellationToken Token)> LoginCalls { get; } = new();
    public List<(StoredAuthTokens Tokens, CancellationToken Token)> LogoutCalls { get; } = new();
    public Func<LoginRequest, CancellationToken, Task<LoginResult>> LoginHandler { get; set; } =
        (_, _) => Task.FromResult(new LoginResult(null, AuthFailure.InvalidCredentials));
    public Func<StoredAuthTokens, CancellationToken, Task<bool>> LogoutHandler { get; set; } =
        (_, _) => Task.FromResult(true);

    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        LoginCalls.Add((request, cancellationToken));
        return LoginHandler(request, cancellationToken);
    }

    public Task<bool> LogoutAsync(StoredAuthTokens tokens, CancellationToken cancellationToken = default)
    {
        LogoutCalls.Add((tokens, cancellationToken));
        return LogoutHandler(tokens, cancellationToken);
    }
}