using FluentAssertions;
using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Resources.Localization;
using JassTournamentManager.App.Tests.Support;
using Xunit;

namespace JassTournamentManager.App.Tests.Features.Authentication;

public class LoginCommandTests
{
    [Fact]
    public void Constructor_NullSession_Throws()
    {
        Action action = () => new LoginFormViewModel(null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("session");
    }

    [Fact]
    public async Task LoginCommand_EmailExceedsMaximumLength_DoesNotCallSession()
    {
        var session = new FakeAuthSession();
        var viewModel = CreateViewModel(session);
        viewModel.Email = new string('a', 309) + "@example.test";
        viewModel.Email.Length.Should().BeGreaterThan(320);

        await viewModel.LoginCommand.ExecuteAsync(null);

        session.LoginCalls.Should().BeEmpty();
        viewModel.ErrorMessage.Should().Be(AppResources.Auth_InvalidInput);
    }

    [Theory]
    [InlineData("", "Secret123!")]
    [InlineData("not-an-email", "Secret123!")]
    [InlineData("player@example.test", "")]
    [InlineData("player@example.test", "   ")]
    public async Task LoginCommand_InvalidInput_DoesNotCallSession(string email, string password)
    {
        var session = new FakeAuthSession();
        var viewModel = new LoginFormViewModel(session) { Email = email, Password = password };

        await viewModel.LoginCommand.ExecuteAsync(null);

        session.LoginCalls.Should().BeEmpty();
        viewModel.ErrorMessage.Should().Be(AppResources.Auth_InvalidInput);
        viewModel.HasError.Should().BeTrue();
        viewModel.Password.Should().Be(password);
    }

    [Fact]
    public async Task LoginCommand_Success_TrimsEmailPreservesPasswordInRequestAndClearsPassword()
    {
        var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        var session = new FakeAuthSession
        {
            LoginHandler = (_, _) => Task.FromResult(new LoginResult(response, AuthFailure.None))
        };
        const string email = "player@example.test";
        const string password = "  Secret123!  ";
        var viewModel = new LoginFormViewModel(session)
        {
            Email = $"  {email}  ", Password = password, ErrorMessage = "Previous failure"
        };

        await viewModel.LoginCommand.ExecuteAsync(null);

        var call = session.LoginCalls.Should().ContainSingle().Subject;
        call.Request.Email.Should().Be(email);
        call.Request.Password.Should().Be(password);
        viewModel.Password.Should().BeEmpty();
        viewModel.ErrorMessage.Should().BeEmpty();
        viewModel.HasError.Should().BeFalse();
    }

    [Theory]
    [InlineData(AuthFailure.InvalidCredentials)]
    [InlineData(AuthFailure.Network)]
    [InlineData(AuthFailure.Server)]
    [InlineData(AuthFailure.InvalidResponse)]
    [InlineData(AuthFailure.None)]
    public async Task LoginCommand_Failure_ShowsLocalizedErrorAndKeepsPassword(AuthFailure failure)
    {
        var session = new FakeAuthSession { LoginHandler = (_, _) => Task.FromResult(new LoginResult(null, failure)) };
        var viewModel = CreateViewModel(session);
        string password = viewModel.Password;
        string expected = failure switch
        {
            AuthFailure.InvalidCredentials => AppResources.Auth_InvalidCredentials,
            AuthFailure.Network => AppResources.Auth_NetworkError,
            AuthFailure.InvalidResponse => AppResources.Auth_InvalidResponse,
            _ => AppResources.Auth_ServerError
        };

        await viewModel.LoginCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be(expected);
        viewModel.Password.Should().Be(password);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoginCommand_Exception_DistinguishesStorageAndUnexpectedErrors(bool storageFailure)
    {
        Exception exception = storageFailure ? new AuthStorageException(new IOException("Storage unavailable")) : new InvalidOperationException("Unexpected");
        var session = new FakeAuthSession { LoginHandler = (_, _) => throw exception };
        var viewModel = CreateViewModel(session);

        await viewModel.LoginCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be(storageFailure ? AppResources.Auth_StorageError : AppResources.Auth_UnexpectedError);
    }

    [Fact]
    public async Task LoginCommand_WhileRunning_DisablesExecutionAndNotifiesBusyState()
    {
        var completion = new TaskCompletionSource<LoginResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeAuthSession { LoginHandler = (_, _) => completion.Task };
        var viewModel = CreateViewModel(session);
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        Task execution = viewModel.LoginCommand.ExecuteAsync(null);

        viewModel.IsBusy.Should().BeTrue();
        viewModel.IsNotBusy.Should().BeFalse();
        viewModel.LoginCommand.CanExecute(null).Should().BeFalse();
        await viewModel.LoginCommand.ExecuteAsync(null);
        session.LoginCalls.Should().ContainSingle();
        viewModel.IsBusy.Should().BeTrue();
        await viewModel.LoginCommand.ExecuteAsync(null);
        session.LoginCalls.Should().ContainSingle();
        completion.SetResult(new LoginResult(null, AuthFailure.InvalidCredentials));
        await execution;

        viewModel.IsBusy.Should().BeFalse();
        viewModel.IsNotBusy.Should().BeTrue();
        viewModel.LoginCommand.CanExecute(null).Should().BeTrue();
        notifications.Count(name => name == nameof(viewModel.IsBusy)).Should().Be(2);
        notifications.Count(name => name == nameof(viewModel.IsNotBusy)).Should().Be(2);
    }

    [Fact]
    public async Task LoginCommand_Canceled_ForwardsCancellationWithoutShowingError()
    {
        var session = new FakeAuthSession
        {
            LoginHandler = async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new LoginResult(null, AuthFailure.None);
            }
        };
        var viewModel = CreateViewModel(session);
        string password = viewModel.Password;
        Task execution = viewModel.LoginCommand.ExecuteAsync(null);

        viewModel.LoginCommand.Cancel();
        await execution;

        session.LoginCalls.Should().ContainSingle().Which.Token.IsCancellationRequested.Should().BeTrue();
        viewModel.ErrorMessage.Should().BeEmpty();
        viewModel.Password.Should().Be(password);
        viewModel.IsBusy.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Session_UserCleared_ClearsPasswordAndDisplaysRemoteWarning(bool warning)
    {
        var session = new FakeAuthSession();
        session.SetUser(AuthTestData.CreateResponse(AuthTestData.CreateTokens()).User);
        var viewModel = CreateViewModel(session);
        viewModel.ErrorMessage = "Old error";

        session.SetUser(null, warning);

        viewModel.Password.Should().BeEmpty();
        viewModel.ErrorMessage.Should().Be(warning ? AppResources.Auth_LogoutRemoteFailed : string.Empty);
    }

    private static LoginFormViewModel CreateViewModel(FakeAuthSession session) => new(session)
    {
        Email = "player@example.test", Password = "Secret123!"
    };
}