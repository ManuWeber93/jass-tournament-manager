using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Tests.Support;
using JassTournamentManager.Contracts.Auth;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure
{
    public sealed class AuthApiClientTests
    {
        private static readonly Uri BaseUri = new("https://api.example.test/");
        private static readonly LoginRequest Credentials = new("login@example.test", "arranged-password");

        [Fact]
        public void Constructor_WithNullHttpClient_Throws()
        {
            Action act = () => new AuthApiClient(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        }

        [Fact]
        public async Task LoginAsync_WithValidResponse_PostsAnonymousCredentialsAndReturnsSession()
        {
            AuthResponse session = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
            var handler = new RecordingHttpMessageHandler(async (request, token) =>
            {
                request.Method.Should().Be(HttpMethod.Post);
                request.RequestUri.Should().Be(new Uri(BaseUri, "api/auth/login"));
                request.Headers.Authorization.Should().BeNull();
                (await request.Content!.ReadFromJsonAsync<LoginRequest>(token)).Should().Be(Credentials);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(session) };
            });
            using var httpClient = CreateHttpClient(handler);

            LoginResult result = await new AuthApiClient(httpClient).LoginAsync(Credentials);

            result.Should().Be(LoginResult.Success(session));
            handler.Requests.Should().ContainSingle();
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, AuthFailure.InvalidCredentials)]
        [InlineData(HttpStatusCode.Unauthorized, AuthFailure.InvalidCredentials)]
        [InlineData(HttpStatusCode.Forbidden, AuthFailure.Server)]
        [InlineData(HttpStatusCode.NotFound, AuthFailure.Server)]
        [InlineData(HttpStatusCode.TooManyRequests, AuthFailure.Server)]
        [InlineData(HttpStatusCode.InternalServerError, AuthFailure.Server)]
        [InlineData(HttpStatusCode.ServiceUnavailable, AuthFailure.Server)]
        [InlineData(HttpStatusCode.Redirect, AuthFailure.Server)]
        public async Task LoginAsync_WithErrorStatus_ReturnsFailureWithoutRetry(HttpStatusCode status, AuthFailure failure)
        {
            var handler = RespondWith(status, "not JSON");
            using var httpClient = CreateHttpClient(handler);

            LoginResult result = await new AuthApiClient(httpClient).LoginAsync(Credentials);

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
        public async Task LoginAsync_WithInvalidJson_ReturnsInvalidResponse(string json)
        {
            using var httpClient = CreateHttpClient(RespondWith(HttpStatusCode.OK, json));

            LoginResult result = await new AuthApiClient(httpClient).LoginAsync(Credentials);

            result.Should().Be(new LoginResult(null, AuthFailure.InvalidResponse));
        }

        public static IEnumerable<object[]> InvalidSessions()
        {
            AuthResponse session = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
            yield return new object[] { session with { AccessToken = null! } };
            yield return new object[] { session with { AccessToken = "" } };
            yield return new object[] { session with { AccessToken = " \t" } };
            yield return new object[] { session with { RefreshToken = null! } };
            yield return new object[] { session with { RefreshToken = "" } };
            yield return new object[] { session with { RefreshToken = " \t" } };
            yield return new object[] { session with { AccessTokenExpiresAt = default } };
            yield return new object[] { session with { RefreshTokenExpiresAt = default } };
            yield return new object[] { session with { User = null! } };
            yield return new object[] { session with { User = session.User with { Id = Guid.Empty } } };
        }

        [Theory]
        [MemberData(nameof(InvalidSessions))]
        public async Task LoginAsync_WithMissingEssentialField_ReturnsInvalidResponse(AuthResponse session)
        {
            var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(session) }));
            using var httpClient = CreateHttpClient(handler);

            LoginResult result = await new AuthApiClient(httpClient).LoginAsync(Credentials);

            result.Should().Be(new LoginResult(null, AuthFailure.InvalidResponse));
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.Accepted, true)]
        [InlineData(HttpStatusCode.NoContent, true)]
        [InlineData(HttpStatusCode.BadRequest, false)]
        [InlineData(HttpStatusCode.Unauthorized, false)]
        [InlineData(HttpStatusCode.Forbidden, false)]
        [InlineData(HttpStatusCode.InternalServerError, false)]
        [InlineData(HttpStatusCode.Redirect, false)]
        public async Task LogoutAsync_PostsTokenSnapshotOnceAndReturnsStatus(HttpStatusCode status, bool expected)
        {
            StoredAuthTokens tokens = AuthTestData.CreateTokens();
            var handler = new RecordingHttpMessageHandler(async (request, token) =>
            {
                request.Method.Should().Be(HttpMethod.Post);
                request.RequestUri.Should().Be(new Uri(BaseUri, "api/auth/logout"));
                request.Headers.Authorization.Should().BeEquivalentTo(new AuthenticationHeaderValue("Bearer", tokens.AccessToken));
                (await request.Content!.ReadFromJsonAsync<LogoutRequest>(token)).Should().Be(new LogoutRequest(tokens.RefreshToken));
                return new HttpResponseMessage(status) { Content = new StringContent("not JSON") };
            });
            using var httpClient = CreateHttpClient(handler);
            var defaultAuthorization = new AuthenticationHeaderValue("Basic", "unrelated");
            httpClient.DefaultRequestHeaders.Authorization = defaultAuthorization;

            bool result = await new AuthApiClient(httpClient).LogoutAsync(tokens);

            result.Should().Be(expected);
            handler.Requests.Should().ContainSingle();
            httpClient.DefaultRequestHeaders.Authorization.Should().BeSameAs(defaultAuthorization);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LoginAsync_WithNetworkFailureOrTimeout_ReturnsNetwork(bool timeout)
        {
            using var httpClient = CreateHttpClient(ThrowOnSend(timeout));

            LoginResult result = await new AuthApiClient(httpClient).LoginAsync(Credentials);

            result.Should().Be(new LoginResult(null, AuthFailure.Network));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LogoutAsync_WithNetworkFailureOrTimeout_ReturnsFalse(bool timeout)
        {
            var handler = ThrowOnSend(timeout);
            using var httpClient = CreateHttpClient(handler);

            bool result = await new AuthApiClient(httpClient).LogoutAsync(AuthTestData.CreateTokens());

            result.Should().BeFalse();
            handler.Requests.Should().ContainSingle();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Authentication_WithCallerCancellation_PropagatesCancellation(bool logout)
        {
            using var cancellation = new CancellationTokenSource();
            var handler = new RecordingHttpMessageHandler((_, token) =>
            {
                token.CanBeCanceled.Should().BeTrue();
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Cancellation was not forwarded.");
            });
            using var httpClient = CreateHttpClient(handler);
            var client = new AuthApiClient(httpClient);
            Func<Task> act = async () =>
            {
                if (logout)
                {
                    await client.LogoutAsync(AuthTestData.CreateTokens(), cancellation.Token);
                }
                else
                {
                    await client.LoginAsync(Credentials, cancellation.Token);
                }
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
            handler.Requests.Should().ContainSingle();
        }

        private static HttpClient CreateHttpClient(HttpMessageHandler handler) => new(handler) { BaseAddress = BaseUri };

        private static RecordingHttpMessageHandler RespondWith(HttpStatusCode status, string content) =>
            new((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(content) }));

        private static RecordingHttpMessageHandler ThrowOnSend(bool timeout) =>
            new((_, _) => Task.FromException<HttpResponseMessage>(timeout
                ? new OperationCanceledException("Arranged timeout.")
                : new HttpRequestException("Arranged network failure.")));
    }
}