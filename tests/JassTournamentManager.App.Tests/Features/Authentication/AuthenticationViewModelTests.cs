using FluentAssertions;
using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Features.Authentication.Models;
using Xunit;

namespace JassTournamentManager.App.Tests.Features.Authentication
{
    public class AuthenticationViewModelTests
    {
        [Fact]
        public void Constructor_WithProvidedForms_StoresSameInstances()
        {
            var loginForm = new LoginFormViewModel();
            var registerFlow = new RegisterFlowViewModel(Array.Empty<ClaimableUserListItem>());

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

        private static AuthenticationViewModel CreateViewModel()
        {
            return new(new LoginFormViewModel(), new RegisterFlowViewModel(Array.Empty<ClaimableUserListItem>()));
        }
    }
}