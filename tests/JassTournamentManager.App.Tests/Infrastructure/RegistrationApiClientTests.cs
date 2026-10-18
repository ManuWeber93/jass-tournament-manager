using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Tests.Support;
using JassTournamentManager.Contracts.Auth;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure;

public sealed class RegistrationApiClientTests
{
    private static readonly Uri BaseUri = new("https://api.example.test/");
    private static readonly RegisterRequest Request = new(Guid.NewGuid(), "register@example.test", "Secret123!", "New", "Player");

    [Fact]
    public async Task RegisterAsync_WithValidResponse_PostsAnonymousPayloadAndReturnsSessionWithoutLogin()
    {
        var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri.Should().Be(new Uri(BaseUri, "api/auth/register"));
            request.Headers.Authorization.Should().BeNull();
            (await request.Content!.ReadFromJsonAsync<RegisterRequest>(token)).Should().Be(Request);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
        });
        using var http = CreateHttpClient(handler);

        LoginResult result = await new AuthApiClient(http).RegisterAsync(Request);

        result.Should().Be(LoginResult.Success(response));
        handler.Requests.Should().ContainSingle();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, AuthFailure.InvalidInput)]
    [InlineData(HttpStatusCode.Conflict, AuthFailure.EmailAlreadyInUse)]
    [InlineData(HttpStatusCode.Forbidden, AuthFailure.UserClaimingDisabled)]
    [InlineData(HttpStatusCode.Unauthorized, AuthFailure.Server)]
    [InlineData(HttpStatusCode.NotFound, AuthFailure.Server)]
    [InlineData(HttpStatusCode.InternalServerError, AuthFailure.Server)]
    public async Task RegisterAsync_WithBackendError_ReturnsFailureWithoutRetry(HttpStatusCode status, AuthFailure failure)
    {
        var handler = RespondWith(status, "not JSON");
        using var http = CreateHttpClient(handler);

        LoginResult result = await new AuthApiClient(http).RegisterAsync(Request);

        result.Should().Be(new LoginResult(null, failure));
        handler.Requests.Should().ContainSingle();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"accessTokenExpiresAt\":\"invalid-date\"}")]
    public async Task RegisterAsync_WithMalformedResponse_ReturnsInvalidResponse(string json)
    {
        using var http = CreateHttpClient(RespondWith(HttpStatusCode.OK, json));

        LoginResult result = await new AuthApiClient(http).RegisterAsync(Request);

        result.Should().Be(new LoginResult(null, AuthFailure.InvalidResponse));
    }

    [Theory]
    [MemberData(nameof(AuthApiClientTests.InvalidSessions), MemberType = typeof(AuthApiClientTests))]
    public async Task RegisterAsync_WithMissingEssentialField_ReturnsInvalidResponse(AuthResponse response)
    {
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) }));
        using var http = CreateHttpClient(handler);

        LoginResult result = await new AuthApiClient(http).RegisterAsync(Request);

        result.Should().Be(new LoginResult(null, AuthFailure.InvalidResponse));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegisterAsync_WithNetworkFailureOrTimeout_ReturnsNetwork(bool timeout)
    {
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromException<HttpResponseMessage>(timeout
            ? new OperationCanceledException("Arranged timeout")
            : new HttpRequestException("Arranged network failure")));
        using var http = CreateHttpClient(handler);

        LoginResult result = await new AuthApiClient(http).RegisterAsync(Request);

        result.Should().Be(new LoginResult(null, AuthFailure.Network));
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task RegisterAsync_WithCallerCancellation_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new RecordingHttpMessageHandler((_, token) =>
        {
            token.CanBeCanceled.Should().BeTrue();
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not forwarded");
        });
        using var http = CreateHttpClient(handler);

        Func<Task> action = () => new AuthApiClient(http).RegisterAsync(Request, cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task RegisterAsync_WithNullRequest_ThrowsWithoutHttpRequest()
    {
        var handler = RespondWith(HttpStatusCode.OK, "{}");
        using var http = CreateHttpClient(handler);

        Func<Task> action = () => new AuthApiClient(http).RegisterAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        handler.Requests.Should().BeEmpty();
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) => new(handler) { BaseAddress = BaseUri };

    private static RecordingHttpMessageHandler RespondWith(HttpStatusCode status, string json) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) }));
}