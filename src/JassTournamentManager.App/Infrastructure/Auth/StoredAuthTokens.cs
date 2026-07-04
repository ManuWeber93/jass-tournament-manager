namespace JassTournamentManager.App.Infrastructure.Auth;

public sealed record StoredAuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);