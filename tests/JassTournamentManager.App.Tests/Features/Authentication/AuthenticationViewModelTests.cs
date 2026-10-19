using FluentAssertions;
using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Features.Authentication.Models;
using JassTournamentManager.App.Features.Home;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Tests.Support;
using Xunit;

namespace JassTournamentManager.App.Tests.Features.Authentication
{
    public class AuthenticationViewModelTests
    {
        [Fact]
        public void Constructor_WithProvidedForms_StoresSameInstances()
        {
            var loginForm = new LoginFormViewModel(new FakeAuthSession());
            var registerFlow = new RegisterFlowViewModel(new FakeAuthSession(), Array.Empty<ClaimableUserListItem>());

            var viewModel = new AuthenticationViewModel(loginForm, registerFlow);

            viewModel.LoginForm.Should().BeSameAs(loginForm);
            viewModel.RegisterFlow.Should().BeSameAs(registerFlow);
        }

        [Fact]
        public void Constructor_ByDefault_ShowsLogin()
        {
            var viewModel = CreateViewModel();

            viewModel.IsLoginMode.Should().BeTrue();
            viewModel.IsRegisterMode.Should().BeFalse();
        }

        [Theory]
        [InlineData(RegisterStep.ClaimableUserSelection)]
        [InlineData(RegisterStep.RegistrationForm)]
        public void ShowRegisterCommand_FromLaterStep_RestartsRegistration(RegisterStep step)
        {
            var viewModel = CreateViewModel();
            viewModel.IsRegisterMode = true;
            viewModel.RegisterFlow.CurrentRegisterStep = step;

            viewModel.ShowRegisterCommand.Execute(null);

            viewModel.IsRegisterMode.Should().BeTrue();
            viewModel.IsLoginMode.Should().BeFalse();
            viewModel.RegisterFlow.CurrentRegisterStep.Should().Be(RegisterStep.PreviousParticipationQuestion);
        }

        [Fact]
        public void ShowLoginCommand_FromRegistration_ShowsLoginWithoutChangingRegistrationStep()
        {
            var viewModel = CreateViewModel();
            viewModel.ShowRegisterCommand.Execute(null);
            viewModel.RegisterFlow.CurrentRegisterStep = RegisterStep.RegistrationForm;

            viewModel.ShowLoginCommand.Execute(null);

            viewModel.IsLoginMode.Should().BeTrue();
            viewModel.IsRegisterMode.Should().BeFalse();
            viewModel.RegisterFlow.CurrentRegisterStep.Should().Be(RegisterStep.RegistrationForm);
        }

        [Fact]
        public void IsRegisterMode_WhenChanged_NotifiesBothModesOnlyOnce()
        {
            var viewModel = CreateViewModel();
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            viewModel.IsRegisterMode = true;
            viewModel.IsRegisterMode = true;

            changedProperties.Should().BeEquivalentTo(new[]
            {
                nameof(AuthenticationViewModel.IsRegisterMode),
                nameof(AuthenticationViewModel.IsLoginMode)
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ModeCommands_WhileLoginRunning_PreserveModeAndRegistrationStep(bool registerMode)
        {
            var completion = new TaskCompletionSource<JassTournamentManager.App.Infrastructure.Auth.LoginResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var session = new FakeAuthSession { LoginHandler = (_, _) => completion.Task };
            var login = new LoginFormViewModel(session) { Email = "player@example.test", Password = "Secret123!" };
            var registration = new RegisterFlowViewModel(new FakeAuthSession(), Array.Empty<ClaimableUserListItem>())
            {
                CurrentRegisterStep = RegisterStep.RegistrationForm
            };
            var viewModel = new AuthenticationViewModel(login, registration) { IsRegisterMode = registerMode };
            Task execution = login.LoginCommand.ExecuteAsync(null);

            viewModel.ShowLoginCommand.Execute(null);
            viewModel.ShowRegisterCommand.Execute(null);

            viewModel.IsRegisterMode.Should().Be(registerMode);
            registration.CurrentRegisterStep.Should().Be(RegisterStep.RegistrationForm);
            completion.SetResult(new(null, JassTournamentManager.App.Infrastructure.Auth.AuthFailure.InvalidCredentials));
            await execution;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ModeCommands_WhileRegistrationRunning_PreserveModeAndRegistrationStep(bool registerMode)
        {
            var completion = new TaskCompletionSource<JassTournamentManager.App.Infrastructure.Auth.LoginResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var session = new FakeAuthSession { RegisterHandler = (_, _) => completion.Task };
            var registration = new RegisterFlowViewModel(session)
            {
                FirstName = "Alex", LastName = "River", Email = "player@example.test",
                RegisterPassword = "Secret123!", ConfirmPassword = "Secret123!",
                CurrentRegisterStep = RegisterStep.RegistrationForm
            };
            var viewModel = new AuthenticationViewModel(new LoginFormViewModel(session), registration)
            {
                IsRegisterMode = registerMode
            };
            Task execution = registration.RegisterCommand.ExecuteAsync(null);

            viewModel.ShowLoginCommand.Execute(null);
            viewModel.ShowRegisterCommand.Execute(null);

            viewModel.IsRegisterMode.Should().Be(registerMode);
            registration.CurrentRegisterStep.Should().Be(RegisterStep.RegistrationForm);
            completion.SetResult(new(null, JassTournamentManager.App.Infrastructure.Auth.AuthFailure.Network));
            await execution;
        }

        [Fact]
        public async Task RegisterCommand_AfterSuccessAndLogout_ShowsLogin()
        {
            var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
            var session = new FakeAuthSession();
            session.RegisterHandler = (_, _) =>
            {
                session.SetUser(response.User);
                return Task.FromResult(new JassTournamentManager.App.Infrastructure.Auth.LoginResult(response, JassTournamentManager.App.Infrastructure.Auth.AuthFailure.None));
            };
            var registration = new RegisterFlowViewModel(session)
            {
                FirstName = "Alex", LastName = "River", Email = "player@example.test",
                RegisterPassword = "Secret123!", ConfirmPassword = "Secret123!"
            };
            var viewModel = new AuthenticationViewModel(new LoginFormViewModel(session), registration)
            {
                IsRegisterMode = true
            };

            await registration.RegisterCommand.ExecuteAsync(null);

            session.CurrentUser.Should().BeSameAs(response.User);
            viewModel.IsRegisterMode.Should().BeTrue();

            viewModel.ResetAfterAuthentication();

            viewModel.IsRegisterMode.Should().BeFalse();
            viewModel.IsLoginMode.Should().BeTrue();

            session.LogoutHandler = _ =>
            {
                session.SetUser(null);
                return Task.CompletedTask;
            };
            var home = new HomeViewModel(session);

            await home.LogoutCommand.ExecuteAsync(null);

            session.CurrentUser.Should().BeNull();
            viewModel.IsLoginMode.Should().BeTrue();
            viewModel.IsRegisterMode.Should().BeFalse();
        }

        [Theory]
        [InlineData(AuthFailure.EmailAlreadyInUse)]
        [InlineData(AuthFailure.Network)]
        [InlineData(AuthFailure.Server)]
        public async Task RegisterCommand_AfterFailure_KeepsRegistrationOpen(AuthFailure failure)
        {
            var session = new FakeAuthSession
            {
                RegisterHandler = (_, _) => Task.FromResult(new LoginResult(null, failure))
            };
            var registration = new RegisterFlowViewModel(session)
            {
                FirstName = "Alex", LastName = "River", Email = "player@example.test",
                RegisterPassword = "Secret123!", ConfirmPassword = "Secret123!",
                CurrentRegisterStep = RegisterStep.RegistrationForm
            };
            var viewModel = new AuthenticationViewModel(new LoginFormViewModel(session), registration)
            {
                IsRegisterMode = true
            };

            await registration.RegisterCommand.ExecuteAsync(null);

            viewModel.IsRegisterMode.Should().BeTrue();
            viewModel.IsLoginMode.Should().BeFalse();
            registration.CurrentRegisterStep.Should().Be(RegisterStep.RegistrationForm);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Authentication_AfterSuccess_PreservesInputsUntilPageReplacementThenResetsBothForms(bool register)
        {
            var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
            var session = new FakeAuthSession
            {
                LoginHandler = (_, _) => Task.FromResult(LoginResult.Success(response)),
                RegisterHandler = (_, _) => Task.FromResult(LoginResult.Success(response))
            };
            var participant = new ClaimableUserListItem(Guid.NewGuid(), "Alex", "River");
            const string loginEmail = "login@example.test";
            const string registrationEmail = "registration@example.test";
            const string password = "Secret123!";
            var login = new LoginFormViewModel(session)
            {
                Email = loginEmail, Password = password, ErrorMessage = "Previous login failure"
            };
            var registration = new RegisterFlowViewModel(session, new[] { participant })
            {
                FirstName = participant.FirstName, LastName = participant.LastName, Email = registrationEmail,
                RegisterPassword = password, ConfirmPassword = password,
                ClaimableUsersFilterText = "not found",
                SelectedClaimableUser = register ? null : participant,
                CurrentRegisterStep = RegisterStep.RegistrationForm,
                InvalidRegisterFormErrorMessage = "Previous registration failure",
                RegisterPasswordMismatchErrorMessage = "Previous mismatch"
            };
            var viewModel = new AuthenticationViewModel(login, registration) { IsRegisterMode = register };
            int resetNotifications = 0;
            viewModel.FormsReset += (_, _) =>
            {
                AssertFormsAreEmpty(viewModel);
                resetNotifications++;
            };

            if (register)
            {
                await registration.RegisterCommand.ExecuteAsync(null);
            }
            else
            {
                await login.LoginCommand.ExecuteAsync(null);
            }

            login.Email.Should().Be(loginEmail);
            login.Password.Should().Be(password);
            registration.Email.Should().Be(registrationEmail);
            registration.FirstName.Should().Be(participant.FirstName);
            registration.LastName.Should().Be(participant.LastName);
            registration.RegisterPassword.Should().Be(password);
            registration.ConfirmPassword.Should().Be(password);
            registration.CurrentRegisterStep.Should().Be(RegisterStep.RegistrationForm);
            viewModel.IsRegisterMode.Should().Be(register);
            resetNotifications.Should().Be(0);

            viewModel.ResetAfterAuthentication();

            AssertFormsAreEmpty(viewModel);
            viewModel.IsLoginMode.Should().BeTrue();
            registration.CurrentRegisterStep.Should().Be(RegisterStep.PreviousParticipationQuestion);
            registration.FilteredClaimableUsers.Should().Equal(participant);
            resetNotifications.Should().Be(1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Authentication_AfterFailure_PreservesBothForms(bool register)
        {
            var session = new FakeAuthSession
            {
                LoginHandler = (_, _) => Task.FromResult(new LoginResult(null, AuthFailure.Network)),
                RegisterHandler = (_, _) => Task.FromResult(new LoginResult(null, AuthFailure.Network))
            };
            const string email = "player@example.test";
            const string password = "Secret123!";
            const string firstName = "Alex";
            const string lastName = "River";
            var login = new LoginFormViewModel(session) { Email = email, Password = password };
            var registration = new RegisterFlowViewModel(session)
            {
                Email = email, FirstName = firstName, LastName = lastName,
                RegisterPassword = password, ConfirmPassword = password
            };
            var viewModel = new AuthenticationViewModel(login, registration) { IsRegisterMode = register };
            int resetNotifications = 0;
            viewModel.FormsReset += (_, _) =>
            {
                AssertFormsAreEmpty(viewModel);
                resetNotifications++;
            };

            if (register)
            {
                await registration.RegisterCommand.ExecuteAsync(null);
            }
            else
            {
                await login.LoginCommand.ExecuteAsync(null);
            }

            login.Email.Should().Be(email);
            login.Password.Should().Be(password);
            registration.Email.Should().Be(email);
            registration.FirstName.Should().Be(firstName);
            registration.LastName.Should().Be(lastName);
            registration.RegisterPassword.Should().Be(password);
            registration.ConfirmPassword.Should().Be(password);
            viewModel.IsRegisterMode.Should().Be(register);
            resetNotifications.Should().Be(0);
        }

        private static void AssertFormsAreEmpty(AuthenticationViewModel viewModel)
        {
            viewModel.LoginForm.Email.Should().BeEmpty();
            viewModel.LoginForm.Password.Should().BeEmpty();
            viewModel.LoginForm.ErrorMessage.Should().BeEmpty();
            viewModel.LoginForm.HasError.Should().BeFalse();
            viewModel.RegisterFlow.Email.Should().BeEmpty();
            viewModel.RegisterFlow.FirstName.Should().BeEmpty();
            viewModel.RegisterFlow.LastName.Should().BeEmpty();
            viewModel.RegisterFlow.RegisterPassword.Should().BeEmpty();
            viewModel.RegisterFlow.ConfirmPassword.Should().BeEmpty();
            viewModel.RegisterFlow.ClaimableUsersFilterText.Should().BeEmpty();
            viewModel.RegisterFlow.SelectedClaimableUser.Should().BeNull();
            viewModel.RegisterFlow.InvalidRegisterFormErrorMessage.Should().BeEmpty();
            viewModel.RegisterFlow.RegisterPasswordMismatchErrorMessage.Should().BeEmpty();
            viewModel.RegisterFlow.IsRegisterFormInvalid.Should().BeFalse();
            viewModel.RegisterFlow.IsRegisterPasswordMismatch.Should().BeFalse();
        }

        private static AuthenticationViewModel CreateViewModel()
        {
            return new(new LoginFormViewModel(new FakeAuthSession()), new RegisterFlowViewModel(new FakeAuthSession(), Array.Empty<ClaimableUserListItem>()));
        }
    }
}