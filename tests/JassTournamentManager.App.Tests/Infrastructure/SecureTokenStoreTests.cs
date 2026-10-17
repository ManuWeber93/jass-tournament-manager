using System.Globalization;
using FluentAssertions;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Tests.Support;
using JassTournamentManager.Contracts.Auth;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure;

public sealed class SecureTokenStoreTests
{
    private const string AccessKey = "auth.accessToken";
    private const string AccessExpiryKey = "auth.accessTokenExpiresAt";
    private const string RefreshKey = "auth.refreshToken";
    private const string RefreshExpiryKey = "auth.refreshTokenExpiresAt";

    [Fact]
    public void Constructor_WithNullStorage_Throws()
    {
        Action act = () => new SecureTokenStore(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("secureStorage");
    }

    [Fact]
    public async Task GetTokensAsync_WithEmptyStorage_ReturnsNull()
    {
        var store = new SecureTokenStore(new FakeSecureStorage());

        (await store.GetTokensAsync()).Should().BeNull();
    }

    public static IEnumerable<object?[]> IncompleteValues()
    {
        foreach (string key in new[] { AccessKey, AccessExpiryKey, RefreshKey, RefreshExpiryKey })
        {
            foreach (string? value in new[] { null, "", " \t" })
            {
                yield return new object?[] { key, value };
            }
        }

        foreach (string key in new[] { AccessExpiryKey, RefreshExpiryKey })
        {
            yield return new object?[] { key, "not-a-date" };
            yield return new object?[] { key, "2031-02-30T12:00:00Z" };
        }
    }

    [Theory]
    [MemberData(nameof(IncompleteValues))]
    public async Task GetTokensAsync_WithMissingOrInvalidField_ReturnsNull(string key, string? value)
    {
        var storage = new FakeSecureStorage();
        var store = new SecureTokenStore(storage);
        await store.SetTokensAsync(AuthTestData.CreateTokens());
        if (value is null)
        {
            storage.Values.Remove(key);
        }
        else
        {
            storage.Values[key] = value;
        }

        (await store.GetTokensAsync()).Should().BeNull();
    }

    [Fact]
    public async Task SetTokensAsync_WithStoredTokens_RoundTripsEveryFieldAndOffset()
    {
        var storage = new FakeSecureStorage();
        var store = new SecureTokenStore(storage);
        StoredAuthTokens tokens = AuthTestData.CreateTokens();

        await store.SetTokensAsync(tokens);
        StoredAuthTokens? actual = await store.GetTokensAsync();

        actual.Should().Be(tokens);
        actual!.AccessTokenExpiresAt.Offset.Should().Be(tokens.AccessTokenExpiresAt.Offset);
        actual.RefreshTokenExpiresAt.Offset.Should().Be(tokens.RefreshTokenExpiresAt.Offset);
        storage.Values.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            [AccessKey] = tokens.AccessToken,
            [AccessExpiryKey] = tokens.AccessTokenExpiresAt.ToString("O", CultureInfo.InvariantCulture),
            [RefreshKey] = tokens.RefreshToken,
            [RefreshExpiryKey] = tokens.RefreshTokenExpiresAt.ToString("O", CultureInfo.InvariantCulture)
        });
    }

    [Fact]
    public async Task SetTokensAsync_WithAuthResponse_MapsAllTokenFields()
    {
        var store = new SecureTokenStore(new FakeSecureStorage());
        StoredAuthTokens expected = AuthTestData.CreateTokens();
        AuthResponse response = AuthTestData.CreateResponse(expected);

        await store.SetTokensAsync(response);

        (await store.GetTokensAsync()).Should().Be(expected);
        (await store.GetAccessTokenAsync()).Should().Be(response.AccessToken);
        (await store.GetRefreshTokenAsync()).Should().Be(response.RefreshToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("standalone-token")]
    public async Task IndividualGetters_WithoutCompleteSession_ReturnRawValues(string? value)
    {
        var storage = new FakeSecureStorage();
        if (value is not null)
        {
            storage.Values[AccessKey] = value;
            storage.Values[RefreshKey] = value;
        }
        var store = new SecureTokenStore(storage);

        (await store.GetAccessTokenAsync()).Should().Be(value);
        (await store.GetRefreshTokenAsync()).Should().Be(value);
    }

    [Fact]
    public async Task SetTokensAsync_WithNullArguments_ThrowsWithoutWriting()
    {
        var storage = new FakeSecureStorage();
        var store = new SecureTokenStore(storage);
        Func<Task> setResponse = () => store.SetTokensAsync((AuthResponse)null!);
        Func<Task> setTokens = () => store.SetTokensAsync((StoredAuthTokens)null!);

        await setResponse.Should().ThrowAsync<ArgumentNullException>().WithParameterName("authResponse");
        await setTokens.Should().ThrowAsync<ArgumentNullException>().WithParameterName("tokens");
        storage.Operations.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAsync_RemovesOnlyAuthenticationKeysAndIsIdempotent()
    {
        var storage = new FakeSecureStorage();
        var store = new SecureTokenStore(storage);
        await store.SetTokensAsync(AuthTestData.CreateTokens());
        const string unrelatedKey = "settings.theme";
        const string unrelatedValue = "dark";
        storage.Values[unrelatedKey] = unrelatedValue;
        storage.Operations.Clear();

        await store.ClearAsync();
        await store.ClearAsync();

        storage.Values.Should().ContainSingle().Which.Should().Be(
            new KeyValuePair<string, string>(unrelatedKey, unrelatedValue));
        storage.Operations.Should().NotContain(nameof(FakeSecureStorage.RemoveAll));
        (await store.GetTokensAsync()).Should().BeNull();
    }

    [Theory]
    [InlineData("GetTokens")]
    [InlineData("GetAccessToken")]
    [InlineData("GetRefreshToken")]
    [InlineData("SetTokens")]
    [InlineData("SetResponse")]
    [InlineData("Clear")]
    public async Task Operations_WithPreCanceledToken_ThrowBeforeAccessingStorage(string operation)
    {
        var storage = new FakeSecureStorage();
        var store = new SecureTokenStore(storage);
        var tokens = AuthTestData.CreateTokens();
        using var source = new CancellationTokenSource();
        source.Cancel();
        CancellationToken token = source.Token;
        Func<Task> act = operation switch
        {
            "GetTokens" => () => store.GetTokensAsync(token),
            "GetAccessToken" => () => store.GetAccessTokenAsync(token),
            "GetRefreshToken" => () => store.GetRefreshTokenAsync(token),
            "SetTokens" => () => store.SetTokensAsync(tokens, token),
            "SetResponse" => () => store.SetTokensAsync(AuthTestData.CreateResponse(tokens), token),
            "Clear" => () => store.ClearAsync(token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        storage.Operations.Should().BeEmpty();
    }
}