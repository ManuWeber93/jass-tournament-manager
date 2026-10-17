using JassTournamentManager.Contracts.Auth;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace JassTournamentManager.App.Infrastructure.Auth;

public sealed class SecureTokenStore : ITokenStore
{
    private const string AccessTokenKey = "auth.accessToken";
    private const string AccessTokenExpiresAtKey = "auth.accessTokenExpiresAt";
    private const string RefreshTokenKey = "auth.refreshToken";
    private const string RefreshTokenExpiresAtKey = "auth.refreshTokenExpiresAt";
    private readonly ISecureStorage secureStorage;

    public SecureTokenStore(ISecureStorage secureStorage)
    {
        this.secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
    }

    public async Task<StoredAuthTokens?> GetTokensAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? accessToken = await secureStorage.GetAsync(AccessTokenKey);
        string? accessTokenExpiresAtValue = await secureStorage.GetAsync(AccessTokenExpiresAtKey);
        string? refreshToken = await secureStorage.GetAsync(RefreshTokenKey);
        string? refreshTokenExpiresAtValue = await secureStorage.GetAsync(RefreshTokenExpiresAtKey);

        if (string.IsNullOrWhiteSpace(accessToken) ||
            string.IsNullOrWhiteSpace(accessTokenExpiresAtValue) ||
            string.IsNullOrWhiteSpace(refreshToken) ||
            string.IsNullOrWhiteSpace(refreshTokenExpiresAtValue) ||
            !TryParseDateTimeOffset(accessTokenExpiresAtValue, out DateTimeOffset accessTokenExpiresAt) ||
            !TryParseDateTimeOffset(refreshTokenExpiresAtValue, out DateTimeOffset refreshTokenExpiresAt))
        {
            return null;
        }

        return new StoredAuthTokens(
            accessToken,
            accessTokenExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await secureStorage.GetAsync(AccessTokenKey);
    }

    public async Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await secureStorage.GetAsync(RefreshTokenKey);
    }

    public Task SetTokensAsync(AuthResponse authResponse, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authResponse);

        return SetTokensAsync(
            new StoredAuthTokens(
                authResponse.AccessToken,
                authResponse.AccessTokenExpiresAt,
                authResponse.RefreshToken,
                authResponse.RefreshTokenExpiresAt),
            cancellationToken);
    }

    public async Task SetTokensAsync(StoredAuthTokens tokens, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        cancellationToken.ThrowIfCancellationRequested();

        await secureStorage.SetAsync(AccessTokenKey, tokens.AccessToken);
        await secureStorage.SetAsync(AccessTokenExpiresAtKey, FormatDateTimeOffset(tokens.AccessTokenExpiresAt));
        await secureStorage.SetAsync(RefreshTokenKey, tokens.RefreshToken);
        await secureStorage.SetAsync(RefreshTokenExpiresAtKey, FormatDateTimeOffset(tokens.RefreshTokenExpiresAt));
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        secureStorage.Remove(AccessTokenKey);
        secureStorage.Remove(AccessTokenExpiresAtKey);
        secureStorage.Remove(RefreshTokenKey);
        secureStorage.Remove(RefreshTokenExpiresAtKey);

        return Task.CompletedTask;
    }

    private static string FormatDateTimeOffset(DateTimeOffset value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static bool TryParseDateTimeOffset(string value, out DateTimeOffset dateTimeOffset)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out dateTimeOffset);
    }
}