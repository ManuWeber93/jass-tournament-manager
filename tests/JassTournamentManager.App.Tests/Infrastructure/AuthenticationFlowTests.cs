using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Features.Home;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Tests.Support;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure;

public class AuthenticationFlowTests
{
    [Fact]
    public async Task LoginThenLogout_WithRealServices_PersistsSessionAndClearsLocalState()
    {
        var tokens = AuthTestData.CreateTokens();
        var response = AuthTestData.CreateResponse(tokens);
        var handler = new RecordingHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/login", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });
            }
            request.RequestUri.AbsolutePath.Should().EndWith("/logout");
            request.Headers.Authorization!.Parameter.Should().Be(tokens.AccessToken);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") };
        var storage = new FakeSecureStorage();
        var store = new SecureTokenStore(storage);
        var session = new AuthSession(new AuthApiClient(http), store);
        var login = new LoginFormViewModel(session) { Email = response.User.Email!, Password = "Secret123!" };
        var home = new HomeViewModel(session);

        await login.LoginCommand.ExecuteAsync(null);

        session.CurrentUser.Should().BeEquivalentTo(response.User);
        (await store.GetTokensAsync()).Should().Be(tokens);
        home.DisplayName.Should().Be($"{response.User.FirstName} {response.User.LastName}");
        login.Password.Should().BeEmpty();

        await home.LogoutCommand.ExecuteAsync(null);

        handler.Requests.Should().HaveCount(2);
        storage.Values.Should().BeEmpty();
        session.CurrentUser.Should().BeNull();
        session.LogoutWarning.Should().BeFalse();
        home.DisplayName.Should().BeEmpty();
        home.HasError.Should().BeFalse();
        login.HasError.Should().BeFalse();
    }
}