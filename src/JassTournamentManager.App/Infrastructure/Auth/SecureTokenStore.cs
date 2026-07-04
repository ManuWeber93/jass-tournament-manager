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

    public async Task<StoredAuthTokens?> GetTokensAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? accessToken = await SecureStorage.GetAsync(AccessTokenKey);
        string? accessTokenExpiresAtValue = await SecureStorage.GetAsync(AccessTokenExpiresAtKey);
        string? refreshToken = await SecureStorage.GetAsync(RefreshTokenKey);
        string? refreshTokenExpiresAtValue = await SecureStorage.GetAsync(RefreshTokenExpiresAtKey);

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
        return await SecureStorage.GetAsync(AccessTokenKey);
    }

    public async Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await SecureStorage.GetAsync(RefreshTokenKey);
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

        await SecureStorage.SetAsync(AccessTokenKey, tokens.AccessToken);
        await SecureStorage.SetAsync(AccessTokenExpiresAtKey, FormatDateTimeOffset(tokens.AccessTokenExpiresAt));
        await SecureStorage.SetAsync(RefreshTokenKey, tokens.RefreshToken);
        await SecureStorage.SetAsync(RefreshTokenExpiresAtKey, FormatDateTimeOffset(tokens.RefreshTokenExpiresAt));
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(AccessTokenExpiresAtKey);
        SecureStorage.Remove(RefreshTokenKey);
        SecureStorage.Remove(RefreshTokenExpiresAtKey);

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