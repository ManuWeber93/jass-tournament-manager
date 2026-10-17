using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using JassTournamentManager.App.Infrastructure.Api;
using JassTournamentManager.App.Tests.Support;
using JassTournamentManager.Contracts.Auth;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure;

public sealed class AuthenticatingHttpHandlerTests
{
    private static readonly Uri ResourceUri = new("https://api.example.test/api/tournaments?active=true");

    [Fact]
    public void Constructor_WithNullTokenStore_Throws()
    {
        Action act = () => new AuthenticatingHttpHandler(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("tokenStore");
    }

    [Fact]
    public async Task SendAsync_WithAccessToken_ReplacesAuthorizationAndReturnsOriginalResponse()
    {
        var store = new FakeTokenStore { AccessToken = "current-access" };
        using var expected = new HttpResponseMessage(HttpStatusCode.Accepted);
        var downstream = new RecordingHttpMessageHandler((_, _) => Task.FromResult(expected));
        using var invoker = CreateInvoker(store, downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "previous");

        using HttpResponseMessage actual = await invoker.SendAsync(request, CancellationToken.None);

        actual.Should().BeSameAs(expected);
        downstream.Requests.Should().ContainSingle().Which.Should().BeSameAs(request);
        request.Headers.Authorization.Should().BeEquivalentTo(new AuthenticationHeaderValue("Bearer", store.AccessToken));
        store.Calls.Select(call => call.Operation).Should().Equal(nameof(FakeTokenStore.GetAccessTokenAsync));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public async Task SendAsync_WithoutAccessToken_PreservesExistingAuthorization(string? accessToken)
    {
        var store = new FakeTokenStore { AccessToken = accessToken };
        var downstream = new RecordingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var invoker = CreateInvoker(store, downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);
        var authorization = new AuthenticationHeaderValue("Basic", "provided");
        request.Headers.Authorization = authorization;

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        request.Headers.Authorization.Should().BeSameAs(authorization);
    }

    [Fact]
    public async Task SendAsync_WithoutAnyAuthorization_DoesNotAddHeader()
    {
        var downstream = new RecordingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var invoker = CreateInvoker(new FakeTokenStore(), downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        request.Headers.Authorization.Should().BeNull();
    }

    [Theory]
    [InlineData("https://api.example.test/api/auth/refresh")]
    [InlineData("https://api.example.test/API/AUTH/REFRESH")]
    public async Task SendAsync_ForExplicitRefresh_PassesThroughWithoutReadingOrClearingTokens(string uri)
    {
        var store = new FakeTokenStore { AccessToken = "unused-access", RefreshToken = "unused-refresh" };
        using var expected = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        var downstream = new RecordingHttpMessageHandler((_, _) => Task.FromResult(expected));
        using var invoker = CreateInvoker(store, downstream);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        var authorization = new AuthenticationHeaderValue("Basic", "explicit");
        request.Headers.Authorization = authorization;

        using var actual = await invoker.SendAsync(request, CancellationToken.None);

        actual.Should().BeSameAs(expected);
        downstream.Requests.Should().ContainSingle().Which.Should().BeSameAs(request);
        request.Headers.Authorization.Should().BeSameAs(authorization);
        store.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public async Task SendAsync_WithUnauthorizedAndNoRefreshToken_ReturnsOriginalResponseWithoutClearing(string? refreshToken)
    {
        var store = new FakeTokenStore { RefreshToken = refreshToken };
        using var expected = new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("original-error") };
        var downstream = new RecordingHttpMessageHandler((_, _) => Task.FromResult(expected));
        using var invoker = CreateInvoker(store, downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);

        using var actual = await invoker.SendAsync(request, CancellationToken.None);

        actual.Should().BeSameAs(expected);
        (await actual.Content.ReadAsStringAsync()).Should().Be("original-error");
        downstream.Requests.Should().ContainSingle();
        store.Calls.Select(call => call.Operation).Should().Equal(
            nameof(FakeTokenStore.GetAccessTokenAsync), nameof(FakeTokenStore.GetRefreshTokenAsync));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task SendAsync_WithSuccessfulRefresh_RotatesTokensAndRetriesOnlyOnce(HttpStatusCode retryStatus)
    {
        var store = new FakeTokenStore { AccessToken = "expired-access", RefreshToken = "previous-refresh" };
        string expectedRefreshToken = store.RefreshToken;
        AuthResponse session = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        using var unauthorized = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        using var expected = new HttpResponseMessage(retryStatus);
        string? refreshBody = null;
        var downstream = new RecordingHttpMessageHandler(async (request, _) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                refreshBody = await request.Content!.ReadAsStringAsync();
                request.Headers.Authorization.Should().BeNull();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(session) };
            }
            return request.Headers.Authorization?.Parameter == session.AccessToken ? expected : unauthorized;
        });
        using var invoker = CreateInvoker(store, downstream);
        using var request = CreateDetailedRequest();
        using var source = new CancellationTokenSource();

        using var actual = await invoker.SendAsync(request, source.Token);

        actual.Should().BeSameAs(expected);
        downstream.Requests.Should().HaveCount(3);
        downstream.Requests[1].Method.Should().Be(HttpMethod.Post);
        using var json = JsonDocument.Parse(refreshBody!);
        json.RootElement.GetProperty("refreshToken").GetString().Should().Be(expectedRefreshToken);
        store.SavedResponse.Should().Be(session);
        store.AccessToken.Should().Be(session.AccessToken);
        store.RefreshToken.Should().Be(session.RefreshToken);
        store.Calls.Select(call => call.Operation).Should().Equal(
            nameof(FakeTokenStore.GetAccessTokenAsync), nameof(FakeTokenStore.GetRefreshTokenAsync),
            nameof(FakeTokenStore.SetTokensAsync));
        store.Calls.Should().OnlyContain(call => call.Token == source.Token);
        downstream.CancellationTokens.Should().OnlyContain(token => token == source.Token);
        await AssertRetryPreservesRequestAsync(request, downstream.Requests[2], session.AccessToken);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.OK, true)]
    public async Task SendAsync_WithFailedOrNullRefresh_ClearsSessionAndReturnsOriginalUnauthorized(HttpStatusCode refreshStatus, bool nullBody)
    {
        var store = new FakeTokenStore { AccessToken = "old-access", RefreshToken = "old-refresh" };
        using var expected = new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("original") };
        int count = 0;
        var downstream = new RecordingHttpMessageHandler((_, _) => Task.FromResult(++count == 1
            ? expected
            : new HttpResponseMessage(refreshStatus)
            {
                Content = new StringContent(nullBody ? "null" : "{}", Encoding.UTF8, "application/json")
            }));
        using var invoker = CreateInvoker(store, downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);
        using var source = new CancellationTokenSource();

        using var actual = await invoker.SendAsync(request, source.Token);

        actual.Should().BeSameAs(expected);
        (await actual.Content.ReadAsStringAsync()).Should().Be("original");
        downstream.Requests.Should().HaveCount(2);
        store.Calls.Select(call => call.Operation).Should().Equal(
            nameof(FakeTokenStore.GetAccessTokenAsync), nameof(FakeTokenStore.GetRefreshTokenAsync), nameof(FakeTokenStore.ClearAsync));
        store.Calls.Should().OnlyContain(call => call.Token == source.Token);
        store.AccessToken.Should().BeNull();
        store.RefreshToken.Should().BeNull();
        store.SavedResponse.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WithMalformedRefreshJson_PropagatesWithoutSavingOrClearingSession()
    {
        var store = new FakeTokenStore { RefreshToken = "existing-refresh" };
        int count = 0;
        var downstream = new RecordingHttpMessageHandler((_, _) => Task.FromResult(++count == 1
            ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json", Encoding.UTF8, "application/json")
            }));
        using var invoker = CreateInvoker(store, downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);
        Func<Task> act = () => invoker.SendAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<JsonException>();
        downstream.Requests.Should().HaveCount(2);
        store.SavedResponse.Should().BeNull();
        store.Calls.Should().NotContain(call => call.Operation == nameof(FakeTokenStore.ClearAsync));
    }

    [Fact]
    public async Task SendAsync_WithSuccessfulRefreshAndNoContent_RetriesWithoutAddingContent()
    {
        var store = new FakeTokenStore { RefreshToken = "existing-refresh" };
        var session = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        int count = 0;
        var downstream = new RecordingHttpMessageHandler((_, _) => Task.FromResult(++count switch
        {
            1 => new HttpResponseMessage(HttpStatusCode.Unauthorized),
            2 => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(session) },
            _ => new HttpResponseMessage(HttpStatusCode.NoContent)
        }));
        using var invoker = CreateInvoker(store, downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        downstream.Requests.Should().HaveCount(3);
        downstream.Requests[2].Content.Should().BeNull();
        downstream.Requests[2].Headers.Authorization!.Parameter.Should().Be(session.AccessToken);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task SendAsync_WithNetworkFailure_PropagatesWithoutAdditionalAttempts(int failingAttempt)
    {
        var store = new FakeTokenStore { RefreshToken = "refresh-before-failure" };
        var failure = new HttpRequestException("arranged-network-failure");
        var session = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        int count = 0;
        var downstream = new RecordingHttpMessageHandler((_, _) =>
        {
            if (++count == failingAttempt)
            {
                throw failure;
            }
            return Task.FromResult(count == 1
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(session) });
        });
        using var invoker = CreateInvoker(store, downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);
        Func<Task> act = () => invoker.SendAsync(request, CancellationToken.None);

        (await act.Should().ThrowAsync<HttpRequestException>()).Which.Should().BeSameAs(failure);
        downstream.Requests.Should().HaveCount(failingAttempt);
        store.Calls.Should().NotContain(call => call.Operation == nameof(FakeTokenStore.ClearAsync));
        store.SavedResponse.Should().Be(failingAttempt == 3 ? session : null);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task SendAsync_WithDownstreamCancellation_PropagatesWithoutClearingOrFurtherRequests(int canceledAttempt)
    {
        var store = new FakeTokenStore { RefreshToken = "refresh-before-cancellation" };
        var session = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        using var source = new CancellationTokenSource();
        int count = 0;
        var downstream = new RecordingHttpMessageHandler((_, token) =>
        {
            if (++count == canceledAttempt)
            {
                source.Cancel();
                token.ThrowIfCancellationRequested();
            }
            return Task.FromResult(count == 1
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(session) });
        });
        using var invoker = CreateInvoker(store, downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);
        Func<Task> act = () => invoker.SendAsync(request, source.Token);

        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.CancellationToken.Should().Be(source.Token);
        downstream.Requests.Should().HaveCount(canceledAttempt);
        store.Calls.Should().NotContain(call => call.Operation == nameof(FakeTokenStore.ClearAsync));
    }

    [Fact]
    public async Task SendAsync_WithPreCanceledToken_DoesNotReachDownstream()
    {
        var downstream = new RecordingHttpMessageHandler((_, _) => throw new InvalidOperationException("Unexpected downstream request."));
        using var invoker = CreateInvoker(new FakeTokenStore(), downstream);
        using var request = new HttpRequestMessage(HttpMethod.Get, ResourceUri);
        using var source = new CancellationTokenSource();
        source.Cancel();
        Func<Task> act = () => invoker.SendAsync(request, source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        downstream.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WithRefresh_UsesAbsoluteUriAcceptedByRealTransport()
    {
        bool reachedTransport = false;
        using var sockets = new SocketsHttpHandler
        {
            UseProxy = false,
            ConnectCallback = (_, _) =>
            {
                reachedTransport = true;
                throw new HttpRequestException("Connection intentionally prevented by test.");
            }
        };
        using var transport = new HttpMessageInvoker(sockets, disposeHandler: false);
        int count = 0;
        var downstream = new RecordingHttpMessageHandler((request, token) => ++count == 1
            ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))
            : transport.SendAsync(request, token));
        using var invoker = CreateInvoker(new FakeTokenStore { RefreshToken = "refresh" }, downstream);
        using var original = new HttpRequestMessage(HttpMethod.Get, ResourceUri);
        Func<Task> act = () => invoker.SendAsync(original, CancellationToken.None);

        // A real transport must accept the URI before the deliberately blocked connection.
        await act.Should().ThrowAsync<HttpRequestException>();
        reachedTransport.Should().BeTrue("refresh requests must use an absolute URI before reaching the transport");
        downstream.Requests[1].RequestUri.Should().Be(new Uri(ResourceUri, "/api/auth/refresh"));
    }

    private static HttpMessageInvoker CreateInvoker(FakeTokenStore store, HttpMessageHandler downstream) =>
        new(new AuthenticatingHttpHandler(store) { InnerHandler = downstream });

    private static HttpRequestMessage CreateDetailedRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, ResourceUri)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            Content = new StringContent("{\"name\":\"Tournament ä\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Correlation-Id", "arranged-correlation");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content.Headers.Add("X-Content-Metadata", "arranged-metadata");
        request.Options.Set(new HttpRequestOptionsKey<object>("context"), new object());
        request.Options.Set(new HttpRequestOptionsKey<int>("attempt-context"), 42);
        return request;
    }

    private static async Task AssertRetryPreservesRequestAsync(HttpRequestMessage original, HttpRequestMessage retry, string accessToken)
    {
        retry.Should().NotBeSameAs(original);
        retry.Method.Should().Be(original.Method);
        retry.RequestUri.Should().Be(original.RequestUri);
        retry.Version.Should().Be(original.Version);
        retry.VersionPolicy.Should().Be(original.VersionPolicy);
        retry.Headers.Authorization.Should().BeEquivalentTo(new AuthenticationHeaderValue("Bearer", accessToken));
        retry.Headers.Where(header => header.Key != "Authorization").Should().BeEquivalentTo(
            original.Headers.Where(header => header.Key != "Authorization"));
        retry.Options.Should().BeEquivalentTo(original.Options);
        retry.Content.Should().NotBeSameAs(original.Content);
        (await retry.Content!.ReadAsByteArrayAsync()).Should().Equal(await original.Content!.ReadAsByteArrayAsync());
        retry.Content.Headers.Should().BeEquivalentTo(original.Content.Headers);
    }
}