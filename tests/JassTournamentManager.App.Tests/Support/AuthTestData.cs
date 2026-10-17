using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.Contracts.Auth;
using JassTournamentManager.Contracts.Users;

namespace JassTournamentManager.App.Tests.Support;

internal static class AuthTestData
{
    public static StoredAuthTokens CreateTokens() => new(
        "arranged-access-token",
        new DateTimeOffset(2031, 4, 5, 12, 30, 15, TimeSpan.FromHours(2)).AddTicks(1234),
        "arranged-refresh-token",
        new DateTimeOffset(2031, 5, 6, 10, 20, 30, TimeSpan.FromHours(-4)).AddTicks(5678));

    public static AuthResponse CreateResponse(StoredAuthTokens tokens) => new(
        tokens.AccessToken, tokens.AccessTokenExpiresAt,
        tokens.RefreshToken, tokens.RefreshTokenExpiresAt,
        new UserResponse(Guid.NewGuid(), "player@example.test", "Test", "Player", false));
}