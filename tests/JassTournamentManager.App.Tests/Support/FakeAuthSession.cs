using System.ComponentModel;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.Contracts.Auth;
using JassTournamentManager.Contracts.Users;

namespace JassTournamentManager.App.Tests.Support;

internal sealed class FakeAuthSession : IAuthSession
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public UserResponse? CurrentUser { get; private set; }
    public bool LogoutWarning { get; private set; }
    public List<(LoginRequest Request, CancellationToken Token)> LoginCalls { get; } = new();
    public List<CancellationToken> LogoutCalls { get; } = new();
    public Func<LoginRequest, CancellationToken, Task<LoginResult>> LoginHandler { get; set; } =
        (_, _) => Task.FromResult(new LoginResult(null, AuthFailure.InvalidCredentials));
    public Func<CancellationToken, Task> LogoutHandler { get; set; } = _ => Task.CompletedTask;

    public void SetUser(UserResponse? user, bool logoutWarning = false)
    {
        CurrentUser = user;
        LogoutWarning = logoutWarning;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUser)));
    }

    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        LoginCalls.Add((request, cancellationToken));
        return LoginHandler(request, cancellationToken);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        LogoutCalls.Add(cancellationToken);
        return LogoutHandler(cancellationToken);
    }
}