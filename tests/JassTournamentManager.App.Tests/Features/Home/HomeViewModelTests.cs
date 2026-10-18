using FluentAssertions;
using JassTournamentManager.App.Features.Home;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Resources.Localization;
using JassTournamentManager.App.Tests.Support;
using Xunit;

namespace JassTournamentManager.App.Tests.Features.Home;

public class HomeViewModelTests
{
    [Fact]
    public void Constructor_NullSession_Throws()
    {
        Action action = () => new HomeViewModel(null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("session");
    }

    [Fact]
    public void Constructor_ByDefault_HasEmptyDisplayAndErrorAndIsIdle()
    {
        var viewModel = new HomeViewModel(new FakeAuthSession());

        viewModel.DisplayName.Should().BeEmpty();
        viewModel.ErrorMessage.Should().BeEmpty();
        viewModel.HasError.Should().BeFalse();
        viewModel.IsBusy.Should().BeFalse();
        viewModel.IsNotBusy.Should().BeTrue();
        viewModel.LogoutCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void DisplayName_TracksSessionUserAndClearsPreviousError()
    {
        var session = new FakeAuthSession();
        var user = AuthTestData.CreateResponse(AuthTestData.CreateTokens()).User;
        session.SetUser(user);
        var viewModel = new HomeViewModel(session);
        viewModel.DisplayName.Should().Be($"{user.FirstName} {user.LastName}");
        viewModel.ErrorMessage = "Old error";
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        session.SetUser(null);

        viewModel.DisplayName.Should().BeEmpty();
        viewModel.ErrorMessage.Should().BeEmpty();
        viewModel.HasError.Should().BeFalse();
        notifications.Should().Contain(nameof(viewModel.DisplayName));
        notifications.Should().Contain(nameof(viewModel.HasError));
    }

    [Fact]
    public async Task LogoutCommand_WhileRunning_DisablesExecutionAndRestoresIdleState()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeAuthSession { LogoutHandler = _ => completion.Task };
        var viewModel = new HomeViewModel(session) { ErrorMessage = "Previous error" };
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        Task execution = viewModel.LogoutCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().BeEmpty();
        viewModel.IsBusy.Should().BeTrue();
        viewModel.IsNotBusy.Should().BeFalse();
        viewModel.LogoutCommand.CanExecute(null).Should().BeFalse();
        await viewModel.LogoutCommand.ExecuteAsync(null);
        session.LogoutCalls.Should().ContainSingle();
        viewModel.IsBusy.Should().BeTrue();
        await viewModel.LogoutCommand.ExecuteAsync(null);
        session.LogoutCalls.Should().ContainSingle();
        completion.SetResult();
        await execution;

        viewModel.IsBusy.Should().BeFalse();
        viewModel.IsNotBusy.Should().BeTrue();
        viewModel.LogoutCommand.CanExecute(null).Should().BeTrue();
        notifications.Count(name => name == nameof(viewModel.IsBusy)).Should().Be(2);
        notifications.Count(name => name == nameof(viewModel.IsNotBusy)).Should().Be(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LogoutCommand_Failure_ShowsLocalizedError(bool storageFailure)
    {
        Exception exception = storageFailure ? new AuthStorageException(new IOException("Storage unavailable")) : new InvalidOperationException("Unexpected");
        var session = new FakeAuthSession { LogoutHandler = _ => throw exception };
        var viewModel = new HomeViewModel(session);

        await viewModel.LogoutCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be(storageFailure ? AppResources.Auth_StorageError : AppResources.Auth_UnexpectedError);
        viewModel.HasError.Should().BeTrue();
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutCommand_Canceled_ForwardsCancellationWithoutError()
    {
        var session = new FakeAuthSession { LogoutHandler = token => Task.Delay(Timeout.Infinite, token) };
        var viewModel = new HomeViewModel(session);
        Task execution = viewModel.LogoutCommand.ExecuteAsync(null);

        viewModel.LogoutCommand.Cancel();
        await execution;

        session.LogoutCalls.Should().ContainSingle().Which.IsCancellationRequested.Should().BeTrue();
        viewModel.ErrorMessage.Should().BeEmpty();
        viewModel.IsBusy.Should().BeFalse();
    }
}