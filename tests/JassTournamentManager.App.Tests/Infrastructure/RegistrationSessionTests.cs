using FluentAssertions;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Tests.Support;
using JassTournamentManager.Contracts.Auth;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure;

public sealed class RegistrationSessionTests
{
    private static readonly RegisterRequest Request = new(null, "register@example.test", "Secret123!", "New", "Player");

    [Fact]
    public async Task RegisterAsync_PersistsBeforePublishingUserAndDoesNotLogin()
    {
        var tokens = AuthTestData.CreateTokens();
        var response = AuthTestData.CreateResponse(tokens);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var store = new SessionTokenStore
        {
            WriteHandler = token =>
            {
                token.Should().Be(cancellation.Token);
                return completion.Task;
            }
        };
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

        Task<LoginResult> registration = session.RegisterAsync(Request, cancellation.Token);

        session.CurrentUser.Should().BeNull();
        notifications.Should().BeEmpty();
        completion.SetResult();
        LoginResult result = await registration;

        result.Session.Should().BeSameAs(response);
        session.CurrentUser.Should().BeSameAs(response.User);
        notifications.Should().ContainSingle().Which.Should().Be(nameof(session.CurrentUser));
        api.RegisterCalls.Should().ContainSingle().Which.Should().Be((Request, cancellation.Token));
        api.LoginCalls.Should().BeEmpty();
    }

    [Theory]
    [InlineData(AuthFailure.InvalidInput)]
    [InlineData(AuthFailure.EmailAlreadyInUse)]
    [InlineData(AuthFailure.UserClaimingDisabled)]
    [InlineData(AuthFailure.Network)]
    [InlineData(AuthFailure.Server)]
    [InlineData(AuthFailure.InvalidResponse)]
    public async Task RegisterAsync_WithFailure_PreservesExistingUserAndTokens(AuthFailure failure)
    {
        var tokens = AuthTestData.CreateTokens();
        var response = AuthTestData.CreateResponse(tokens);
        var api = SuccessfulApi(response);
        var store = new SessionTokenStore();
        var session = new AuthSession(api, store);
        await session.RegisterAsync(Request);
        var notifications = new List<string?>();
        session.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        var expected = new LoginResult(null, failure);
        api.RegisterHandler = (_, _) => Task.FromResult(expected);
        store.WriteHandler = _ => throw new InvalidOperationException("Storage must not be touched");

        LoginResult result = await session.RegisterAsync(Request);

        result.Should().BeSameAs(expected);
        session.CurrentUser.Should().BeSameAs(response.User);
        store.Tokens.Should().Be(tokens);
        store.ClearCalls.Should().BeEmpty();
        notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithStorageFailure_ClearsPartialTokensWithoutPublishingUser()
    {
        var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        var failure = new InvalidOperationException("Cannot persist registration");
        var store = new SessionTokenStore { WriteHandler = _ => Task.FromException(failure) };
        var session = new AuthSession(SuccessfulApi(response), store);
        var notifications = new List<string?>();
        session.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        Func<Task> action = () => session.RegisterAsync(Request);

        (await action.Should().ThrowAsync<AuthStorageException>()).Which.InnerException.Should().BeSameAs(failure);
        store.ClearCalls.Should().Equal(CancellationToken.None);
        store.Tokens.Should().BeNull();
        session.CurrentUser.Should().BeNull();
        notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithStorageCancellation_ClearsPartialTokensAndPropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        var store = new SessionTokenStore
        {
            WriteHandler = token =>
            {
                token.Should().Be(cancellation.Token);
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }
        };
        var session = new AuthSession(SuccessfulApi(response), store);

        Func<Task> action = () => session.RegisterAsync(Request, cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        store.ClearCalls.Should().Equal(CancellationToken.None);
        store.Tokens.Should().BeNull();
        session.CurrentUser.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithApiCancellation_DoesNotPersistOrPublishUser()
    {
        using var cancellation = new CancellationTokenSource();
        var api = new FakeAuthApiClient
        {
            RegisterHandler = (_, token) =>
            {
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Cancellation was not forwarded");
            }
        };
        var store = new SessionTokenStore { WriteHandler = _ => throw new InvalidOperationException("Unexpected write") };
        var session = new AuthSession(api, store);

        Func<Task> action = () => session.RegisterAsync(Request, cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        api.RegisterCalls.Should().ContainSingle().Which.Should().Be((Request, cancellation.Token));
        store.ClearCalls.Should().BeEmpty();
        store.Tokens.Should().BeNull();
        session.CurrentUser.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_AfterRemoteLogoutWarning_ResetsWarning()
    {
        var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        var api = SuccessfulApi(response);
        api.LogoutHandler = (_, _) => Task.FromResult(false);
        var session = new AuthSession(api, new SessionTokenStore());
        await session.RegisterAsync(Request);
        await session.LogoutAsync();
        session.LogoutWarning.Should().BeTrue();

        await session.RegisterAsync(Request);

        session.LogoutWarning.Should().BeFalse();
        session.CurrentUser.Should().BeSameAs(response.User);
    }

    private static FakeAuthApiClient SuccessfulApi(AuthResponse response) => new()
    {
        RegisterHandler = (_, _) => Task.FromResult(LoginResult.Success(response))
    };
}