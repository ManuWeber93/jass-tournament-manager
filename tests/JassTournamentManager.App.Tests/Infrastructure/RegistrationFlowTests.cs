using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Tests.Support;
using JassTournamentManager.Contracts.Auth;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure;

public sealed class RegistrationFlowTests
{
    [Fact]
    public async Task RegisterThenLogout_WithRealServices_PersistsSessionAndClearsLocalState()
    {
        var tokens = AuthTestData.CreateTokens();
        var response = AuthTestData.CreateResponse(tokens);
        var registration = new RegisterRequest(null, response.User.Email!, "Secret123!", response.User.FirstName, response.User.LastName);
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            if (request.RequestUri!.AbsolutePath == "/api/auth/register")
            {
                request.Headers.Authorization.Should().BeNull();
                (await request.Content!.ReadFromJsonAsync<RegisterRequest>(token)).Should().Be(registration);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
            }

            request.RequestUri.AbsolutePath.Should().Be("/api/auth/logout");
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be(tokens.AccessToken);
            (await request.Content!.ReadFromJsonAsync<LogoutRequest>(token)).Should().Be(new LogoutRequest(tokens.RefreshToken));
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") };
        var storage = new FakeSecureStorage();
        var store = new SecureTokenStore(storage);
        var session = new AuthSession(new AuthApiClient(http), store);

        LoginResult result = await session.RegisterAsync(registration);

        result.Should().Be(LoginResult.Success(response));
        session.CurrentUser.Should().BeEquivalentTo(response.User);
        (await store.GetTokensAsync()).Should().Be(tokens);
        handler.Requests.Should().ContainSingle();

        await session.LogoutAsync();

        handler.Requests.Should().HaveCount(2);
        storage.Values.Should().BeEmpty();
        session.CurrentUser.Should().BeNull();
        session.LogoutWarning.Should().BeFalse();
    }
}