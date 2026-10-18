using FluentAssertions;
using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Features.Authentication.Models;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Resources.Localization;
using JassTournamentManager.App.Tests.Support;
using Xunit;

namespace JassTournamentManager.App.Tests.Features.Authentication;

public class RegisterCommandTests
{
    [Fact]
    public void Constructor_NullSession_Throws()
    {
        Action action = () => new RegisterFlowViewModel(null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("session");
    }

    [Fact]
    public void Constructor_NullClaimableUsers_Throws()
    {
        Action action = () => new RegisterFlowViewModel(new FakeAuthSession(), null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("claimableUsers");
    }

    [Fact]
    public async Task RegisterCommand_Success_NormalizesNamesAndEmailPreservesPasswordAndClearsBothPasswords()
    {
        var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
        var session = new FakeAuthSession
        {
            RegisterHandler = (_, _) => Task.FromResult(new LoginResult(response, AuthFailure.None))
        };
        const string email = "player@example.test";
        const string firstName = "Alex";
        const string lastName = "River";
        const string password = "  Secret123!  ";
        var viewModel = new RegisterFlowViewModel(session)
        {
            Email = $"  {email}  ", FirstName = $"  {firstName}  ", LastName = $"  {lastName}  ",
            RegisterPassword = password, ConfirmPassword = password,
            InvalidRegisterFormErrorMessage = "Previous failure"
        };

        await viewModel.RegisterCommand.ExecuteAsync(null);

        var call = session.RegisterCalls.Should().ContainSingle().Subject;
        call.Request.Email.Should().Be(email);
        call.Request.FirstName.Should().Be(firstName);
        call.Request.LastName.Should().Be(lastName);
        call.Request.Password.Should().Be(password);
        call.Request.ClaimedUserId.Should().BeNull();
        viewModel.RegisterPassword.Should().BeEmpty();
        viewModel.ConfirmPassword.Should().BeEmpty();
        viewModel.InvalidRegisterFormErrorMessage.Should().BeEmpty();
        viewModel.IsRegisterFormInvalid.Should().BeFalse();
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCommand_InvalidForm_DoesNotCallSession()
    {
        var session = new FakeAuthSession();
        var viewModel = CreateViewModel(session);
        viewModel.Email = "not-an-email";

        await viewModel.RegisterCommand.ExecuteAsync(null);

        session.RegisterCalls.Should().BeEmpty();
        viewModel.InvalidRegisterFormErrorMessage.Should().Be(AppResources.Register_ErrorMessage_RegisterFormInvalid);
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCommand_SelectedUser_DoesNotSubmitClaimOrCreateUnclaimedAccount()
    {
        var user = new ClaimableUserListItem(Guid.NewGuid(), "Alex", "River");
        var session = new FakeAuthSession();
        var viewModel = CreateViewModel(session);
        viewModel.SelectedClaimableUser = user;

        await viewModel.RegisterCommand.ExecuteAsync(null);

        session.RegisterCalls.Should().BeEmpty();
        viewModel.SelectedClaimableUser.Should().BeSameAs(user);
        viewModel.InvalidRegisterFormErrorMessage.Should().Be(AppResources.Register_UserClaimingUnavailable);
    }

    [Theory]
    [InlineData(AuthFailure.InvalidInput)]
    [InlineData(AuthFailure.EmailAlreadyInUse)]
    [InlineData(AuthFailure.UserClaimingDisabled)]
    [InlineData(AuthFailure.Network)]
    [InlineData(AuthFailure.Server)]
    [InlineData(AuthFailure.InvalidResponse)]
    public async Task RegisterCommand_Failure_ShowsLocalizedErrorAndKeepsPasswords(AuthFailure failure)
    {
        var session = new FakeAuthSession { RegisterHandler = (_, _) => Task.FromResult(new LoginResult(null, failure)) };
        var viewModel = CreateViewModel(session);
        string password = viewModel.RegisterPassword;
        string confirmation = viewModel.ConfirmPassword;
        string expected = failure switch
        {
            AuthFailure.InvalidInput => AppResources.Register_ErrorMessage_RegisterFormInvalid,
            AuthFailure.EmailAlreadyInUse => AppResources.Register_EmailAlreadyInUse,
            AuthFailure.UserClaimingDisabled => AppResources.Register_UserClaimingUnavailable,
            AuthFailure.Network => AppResources.Register_NetworkError,
            AuthFailure.Server => AppResources.Auth_ServerError,
            _ => AppResources.Register_InvalidResponse
        };

        await viewModel.RegisterCommand.ExecuteAsync(null);

        viewModel.InvalidRegisterFormErrorMessage.Should().Be(expected);
        viewModel.IsRegisterFormInvalid.Should().BeTrue();
        viewModel.RegisterPassword.Should().Be(password);
        viewModel.ConfirmPassword.Should().Be(confirmation);
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCommand_StorageFailure_ShowsRegistrationSpecificMessage()
    {
        var session = new FakeAuthSession
        {
            RegisterHandler = (_, _) => throw new AuthStorageException(new IOException("Storage unavailable"))
        };
        var viewModel = CreateViewModel(session);

        await viewModel.RegisterCommand.ExecuteAsync(null);

        viewModel.InvalidRegisterFormErrorMessage.Should().Be(AppResources.Register_StorageError);
        viewModel.RegisterPassword.Should().BeEmpty();
        viewModel.ConfirmPassword.Should().BeEmpty();
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCommand_Canceled_ForwardsCancellationAndResetsBusyWithoutError()
    {
        var session = new FakeAuthSession
        {
            RegisterHandler = async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new LoginResult(null, AuthFailure.None);
            }
        };
        var viewModel = CreateViewModel(session);
        string password = viewModel.RegisterPassword;
        Task execution = viewModel.RegisterCommand.ExecuteAsync(null);

        viewModel.RegisterCommand.Cancel();
        await execution;

        session.RegisterCalls.Should().ContainSingle().Which.Token.IsCancellationRequested.Should().BeTrue();
        viewModel.InvalidRegisterFormErrorMessage.Should().BeEmpty();
        viewModel.RegisterPassword.Should().Be(password);
        viewModel.ConfirmPassword.Should().Be(password);
        viewModel.IsBusy.Should().BeFalse();
        viewModel.IsNotBusy.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterCommand_WhileRunning_BlocksRepeatedExecutionAndFreezesNavigation()
    {
        var completion = new TaskCompletionSource<LoginResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeAuthSession { RegisterHandler = (_, _) => completion.Task };
        var viewModel = CreateViewModel(session);
        viewModel.CurrentRegisterStep = RegisterStep.RegistrationForm;
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        Task execution = viewModel.RegisterCommand.ExecuteAsync(null);

        viewModel.IsBusy.Should().BeTrue();
        viewModel.IsNotBusy.Should().BeFalse();
        viewModel.RegisterCommand.CanExecute(null).Should().BeFalse();
        await viewModel.RegisterCommand.ExecuteAsync(null);
        await viewModel.RegisterCommand.ExecuteAsync(null);
        viewModel.RegisterNavigateBackCommand.Execute(null);
        viewModel.ShowClaimableUserSelectionCommand.Execute(null);
        viewModel.NoUserClaimingCommand.Execute(null);
        viewModel.ContinueWithSelectedClaimableUserCommand.Execute(null);

        session.RegisterCalls.Should().ContainSingle();
        viewModel.CurrentRegisterStep.Should().Be(RegisterStep.RegistrationForm);
        viewModel.IsBusy.Should().BeTrue();
        completion.SetResult(new LoginResult(null, AuthFailure.Network));
        await execution;

        viewModel.IsBusy.Should().BeFalse();
        viewModel.IsNotBusy.Should().BeTrue();
        viewModel.RegisterCommand.CanExecute(null).Should().BeTrue();
        notifications.Count(name => name == nameof(viewModel.IsBusy)).Should().Be(2);
        notifications.Count(name => name == nameof(viewModel.IsNotBusy)).Should().Be(2);
    }

    [Fact]
    public void Session_UserCleared_ClearsPasswordsAndErrors()
    {
        var session = new FakeAuthSession();
        session.SetUser(AuthTestData.CreateResponse(AuthTestData.CreateTokens()).User);
        var viewModel = CreateViewModel(session);
        viewModel.ConfirmPassword = "Different123!";
        viewModel.InvalidRegisterFormErrorMessage = "Previous failure";

        session.SetUser(null);

        viewModel.RegisterPassword.Should().BeEmpty();
        viewModel.ConfirmPassword.Should().BeEmpty();
        viewModel.RegisterPasswordMismatchErrorMessage.Should().BeEmpty();
        viewModel.InvalidRegisterFormErrorMessage.Should().BeEmpty();
    }

    private static RegisterFlowViewModel CreateViewModel(FakeAuthSession session) => new(session)
    {
        FirstName = "Alex", LastName = "River", Email = "player@example.test",
        RegisterPassword = "Secret123!", ConfirmPassword = "Secret123!"
    };
}