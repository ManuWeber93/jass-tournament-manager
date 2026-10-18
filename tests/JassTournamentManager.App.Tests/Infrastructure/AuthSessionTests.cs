using FluentAssertions;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Tests.Support;
using JassTournamentManager.Contracts.Auth;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure;

public class AuthSessionTests
{
    private static readonly LoginRequest Request = new("player@example.test", "Secret123!");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_NullDependency_Throws(bool missingClient)
    {
        Action action = () => new AuthSession(missingClient ? null! : new FakeAuthApiClient(),
            missingClient ? new SessionTokenStore() : null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName(missingClient ? "apiClient" : "tokenStore");
    }

    [Fact]
    public void Constructor_ByDefault_HasNoUserOrWarning()
    {
        var session = new AuthSession(new FakeAuthApiClient(), new SessionTokenStore());

        session.CurrentUser.Should().BeNull();
        session.LogoutWarning.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_PersistsBeforePublishingUser()
    {
        var tokens = AuthTestData.CreateTokens();
        var response = AuthTestData.CreateResponse(tokens);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new SessionTokenStore { WriteHandler = _ => completion.Task };
        var api = SuccessfulApi(response);
        var session = new AuthSession(api, store);
        var notifications = new List<string?>();
        session.PropertyChanged += (_, args) =>
        {
            notifications.Add(args.PropertyName);
            if (args.PropertyName == nameof(session.CurrentUser))
            {
                completion.Task.IsCompletedSuccessfully.Should().BeTrue();
                store.Tokens.Should().Be(tokens);
            }
        };
        using var cancellation = new CancellationTokenSource();

        Task<LoginResult> login = session.LoginAsync(Request, cancellation.Token);

        session.CurrentUser.Should().BeNull();
        notifications.Should().BeEmpty();
        completion.SetResult();
        LoginResult result = await login;

        result.Session.Should().BeSameAs(response);
        session.CurrentUser.Should().BeSameAs(response.User);
        notifications.Should().ContainSingle().Which.Should().Be(nameof(session.CurrentUser));
        api.LoginCalls.Should().ContainSingle().Which.Should().Be((Request, cancellation.Token));
    }

    [Theory]
    [InlineData(AuthFailure.InvalidCredentials)]
    [InlineData(AuthFailure.Network)]
    [InlineData(AuthFailure.Server)]
    [InlineData(AuthFailure.InvalidResponse)]
    public async Task LoginAsync_Failure_PreservesExistingUserAndTokens(AuthFailure failure)
    {
        var tokens = AuthTestData.CreateTokens();
        var response = AuthTestData.CreateResponse(tokens);
        var store = new SessionTokenStore();
        var api = SuccessfulApi(response);
        var session = new AuthSession(api, store);
        await session.LoginAsync(Request);
        var notifications = new List<string?>();
        session.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        var expected = new LoginResult(null, failure);
        api.LoginHandler = (_, _) => Task.FromResult(expected);

        LoginResult result = await session.LoginAsync(Request);

        result.Should().Be(expected);
        session.CurrentUser.Should().BeSameAs(response.User);
        store.Tokens.Should().Be(tokens);
        store.ClearCalls.Should().BeEmpty();
        notifications.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoginAsync_PartialWriteFailure_ClearsWithNoncancelableToken(bool canceled)
    {
        using var cancellation = new CancellationTokenSource();
        var failure = new InvalidOperationException("Partial write failed");
        var store = new SessionTokenStore
        {
            WriteHandler = token =>
            {
                if (canceled)
                {
                    cancellation.Cancel();
                    token.ThrowIfCancellationRequested();
                }
                throw failure;
            }
        };
        var session = new AuthSession(SuccessfulApi(AuthTestData.CreateResponse(AuthTestData.CreateTokens())), store);

        Func<Task> action = () => session.LoginAsync(Request, cancellation.Token);

        if (canceled)
        {
            await action.Should().ThrowAsync<OperationCanceledException>();
        }
        else
        {
            (await action.Should().ThrowAsync<AuthStorageException>()).Which.InnerException.Should().BeSameAs(failure);
        }
        store.Tokens.Should().BeNull();
        store.ClearCalls.Should().Equal(CancellationToken.None);
        session.CurrentUser.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LogoutAsync_UsesLatestStoredTokensAndClearsLocalSession(bool revoked)
    {
        var api = SuccessfulApi(AuthTestData.CreateResponse(AuthTestData.CreateTokens()));
        api.LogoutHandler = (_, _) => Task.FromResult(revoked);
        var store = new SessionTokenStore();
        var session = new AuthSession(api, store);
        await session.LoginAsync(Request);
        var latest = AuthTestData.CreateTokens() with { AccessToken = "rotated-access", RefreshToken = "rotated-refresh" };
        store.Tokens = latest;
        using var cancellation = new CancellationTokenSource();

        await session.LogoutAsync(cancellation.Token);

        api.LogoutCalls.Should().ContainSingle().Which.Should().Be((latest, cancellation.Token));
        store.ClearCalls.Should().Equal(CancellationToken.None);
        store.Tokens.Should().BeNull();
        session.CurrentUser.Should().BeNull();
        session.LogoutWarning.Should().Be(!revoked);
    }

    [Fact]
    public async Task LogoutAsync_WithoutTokens_DoesNotCallRemoteOrWarn()
    {
        var api = new FakeAuthApiClient();
        var store = new SessionTokenStore();
        var session = new AuthSession(api, store);

        await session.LogoutAsync();

        api.LogoutCalls.Should().BeEmpty();
        store.ClearCalls.Should().Equal(CancellationToken.None);
        session.LogoutWarning.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LogoutAsync_RemoteThrows_StillClearsLocalSession(bool canceled)
    {
        using var cancellation = new CancellationTokenSource();
        var api = SuccessfulApi(AuthTestData.CreateResponse(AuthTestData.CreateTokens()));
        api.LogoutHandler = (_, token) =>
        {
            if (canceled)
            {
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
            }
            throw new HttpRequestException("Offline");
        };
        var store = new SessionTokenStore();
        var session = new AuthSession(api, store);
        await session.LoginAsync(Request);

        Func<Task> action = () => session.LogoutAsync(cancellation.Token);

        await action.Should().ThrowAsync<Exception>();
        store.ClearCalls.Should().Equal(CancellationToken.None);
        store.Tokens.Should().BeNull();
        session.CurrentUser.Should().BeNull();
        session.LogoutWarning.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_ClearFails_ReportsStorageErrorAndRetainsUser()
    {
        var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        var failure = new InvalidOperationException("Cannot clear storage");
        var store = new SessionTokenStore { ClearException = failure };
        var session = new AuthSession(SuccessfulApi(response), store);
        await session.LoginAsync(Request);

        Func<Task> action = () => session.LogoutAsync();

        (await action.Should().ThrowAsync<AuthStorageException>()).Which.InnerException.Should().BeSameAs(failure);
        session.CurrentUser.Should().BeSameAs(response.User);
    }

    [Fact]
    public async Task LoginAsync_AfterRemoteLogoutWarning_ResetsWarning()
    {
        var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        var api = SuccessfulApi(response);
        api.LogoutHandler = (_, _) => Task.FromResult(false);
        var session = new AuthSession(api, new SessionTokenStore());
        await session.LoginAsync(Request);
        await session.LogoutAsync();
        session.LogoutWarning.Should().BeTrue();

        await session.LoginAsync(Request);

        session.LogoutWarning.Should().BeFalse();
        session.CurrentUser.Should().BeSameAs(response.User);
    }

    [Fact]
    public async Task LogoutAsync_AlreadyCanceled_StillClearsLocalSession()
    {
        var store = new SessionTokenStore();
        var api = SuccessfulApi(AuthTestData.CreateResponse(AuthTestData.CreateTokens()));
        var session = new AuthSession(api, store);
        await session.LoginAsync(Request);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> action = () => session.LogoutAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        api.LogoutCalls.Should().BeEmpty();
        store.ClearCalls.Should().Equal(CancellationToken.None);
        store.Tokens.Should().BeNull();
        session.CurrentUser.Should().BeNull();
    }

    private static FakeAuthApiClient SuccessfulApi(AuthResponse response) => new()
    {
        LoginHandler = (_, _) => Task.FromResult(new LoginResult(response, AuthFailure.None))
    };
}