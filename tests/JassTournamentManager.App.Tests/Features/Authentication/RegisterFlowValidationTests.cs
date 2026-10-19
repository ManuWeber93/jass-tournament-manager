using FluentAssertions;
using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Features.Authentication.Models;
using JassTournamentManager.App.Resources.Localization;
using JassTournamentManager.App.Tests.Support;
using Xunit;

namespace JassTournamentManager.App.Tests.Features.Authentication
{
    public class RegisterFlowValidationTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("Secret12!", "")]
        [InlineData("", "Secret12!")]
        [InlineData("Secret12!", "Secret12!")]
        public void Passwords_WithMissingOrMatchingValues_DoNotShowMismatch(string password, string confirmation)
        {
            var viewModel = CreateValidViewModel();
            viewModel.ValidatePasswordConfirmationCommand.Execute(null);

            viewModel.RegisterPassword = password;
            viewModel.ConfirmPassword = confirmation;

            viewModel.RegisterPasswordMismatchErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterPasswordMismatch.Should().BeFalse();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Passwords_AfterConfirmationWasLeft_WhenEitherValueChangesToMismatch_ShowResourceMessage(bool changePassword)
        {
            var viewModel = CreateValidViewModel();
            viewModel.ValidatePasswordConfirmationCommand.Execute(null);

            if (changePassword)
            {
                viewModel.RegisterPassword = "Different12!";
            }
            else
            {
                viewModel.ConfirmPassword = "Different12!";
            }

            viewModel.RegisterPasswordMismatchErrorMessage.Should().Be(AppResources.Register_ErrorMessage_PasswordMismatch);
            viewModel.IsRegisterPasswordMismatch.Should().BeTrue();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Passwords_WhenMismatchIsCorrectedOrValueCleared_ClearError(bool changePassword, bool clearValue)
        {
            var viewModel = CreateValidViewModel();
            viewModel.ValidatePasswordConfirmationCommand.Execute(null);
            viewModel.ConfirmPassword = "Different12!";
            viewModel.IsRegisterPasswordMismatch.Should().BeTrue();

            if (changePassword)
            {
                viewModel.RegisterPassword = clearValue ? string.Empty : viewModel.ConfirmPassword;
            }
            else
            {
                viewModel.ConfirmPassword = clearValue ? string.Empty : viewModel.RegisterPassword;
            }

            viewModel.RegisterPasswordMismatchErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterPasswordMismatch.Should().BeFalse();
        }

        [Fact]
        public void PasswordMismatch_WhenIntroducedAndCorrected_NotifiesMessageAndFlag()
        {
            var viewModel = CreateValidViewModel();
            viewModel.ValidatePasswordConfirmationCommand.Execute(null);
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            viewModel.ConfirmPassword = "Different12!";
            viewModel.ConfirmPassword = viewModel.RegisterPassword;

            changedProperties.Should().BeEquivalentTo(new[]
            {
                nameof(RegisterFlowViewModel.ConfirmPassword),
                nameof(RegisterFlowViewModel.RegisterPasswordMismatchErrorMessage),
                nameof(RegisterFlowViewModel.IsRegisterPasswordMismatch),
                nameof(RegisterFlowViewModel.ConfirmPassword),
                nameof(RegisterFlowViewModel.RegisterPasswordMismatchErrorMessage),
                nameof(RegisterFlowViewModel.IsRegisterPasswordMismatch)
            });
        }

        [Theory]
        [InlineData(true, "")]
        [InlineData(true, "   ")]
        [InlineData(false, "")]
        [InlineData(false, "\t")]
        public async Task RegisterCommand_WithBlankName_ShowsInvalidFormResource(bool firstName, string value)
        {
            var viewModel = CreateValidViewModel();
            SetName(viewModel, firstName, value);

            await AssertInvalidRegistration(viewModel);
        }

        [Theory]
        [InlineData(true, 1, true)]
        [InlineData(true, 50, true)]
        [InlineData(true, 51, false)]
        [InlineData(false, 1, true)]
        [InlineData(false, 50, true)]
        [InlineData(false, 51, false)]
        public async Task RegisterCommand_WithNameLength_ValidatesBoundary(bool firstName, int length, bool valid)
        {
            var viewModel = CreateValidViewModel();
            SetName(viewModel, firstName, new string('a', length));

            await AssertRegistrationResult(viewModel, valid);
        }

        [Theory]
        [InlineData(319, true)]
        [InlineData(320, true)]
        [InlineData(321, false)]
        public async Task RegisterCommand_WithEmailLength_ValidatesBoundary(int length, bool valid)
        {
            var viewModel = CreateValidViewModel();
            const string suffix = "@example.org";
            viewModel.Email = new string('a', length - suffix.Length) + suffix;

            await AssertRegistrationResult(viewModel, valid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-an-email")]
        [InlineData("player@")]
        [InlineData("@example.org")]
        [InlineData("Player <player@example.org>")]
        public async Task RegisterCommand_WithInvalidEmail_ShowsInvalidFormResource(string email)
        {
            var viewModel = CreateValidViewModel();
            viewModel.Email = email;

            await AssertInvalidRegistration(viewModel);
        }

        [Theory]
        [InlineData("player+event@example.org")]
        [InlineData("PLAYER@EXAMPLE.ORG")]
        [InlineData("first.last@example.org")]
        [InlineData(" player@example.org ")]
        public async Task RegisterCommand_WithValidEmail_ClearsPreviousError(string email)
        {
            var viewModel = CreateValidViewModel();
            viewModel.Email = email;

            await AssertRegistrationResult(viewModel, true);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("        ", false)]
        [InlineData("Abcd1!x", false)]
        [InlineData("Abcde1!x", true)]
        [InlineData("Abcdefg!", false)]
        [InlineData("Abcdefg1", false)]
        [InlineData("Abcdef1_", false)]
        [InlineData("Abcdef1 ", false)]
        [InlineData("1234567!", true)]
        public async Task RegisterCommand_WithPassword_ValidatesLengthDigitAndSpecialCharacter(string password, bool valid)
        {
            var viewModel = CreateValidViewModel();
            viewModel.RegisterPassword = password;
            viewModel.ConfirmPassword = password;

            await AssertRegistrationResult(viewModel, valid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Different12!")]
        [InlineData("secret12!")]
        public async Task RegisterCommand_WithMissingOrDifferentConfirmation_ShowsInvalidFormResource(string confirmation)
        {
            var viewModel = CreateValidViewModel();
            viewModel.ConfirmPassword = confirmation;

            await AssertInvalidRegistration(viewModel);
        }

        [Fact]
        public async Task RegisterCommand_AfterCorrectingInvalidForm_ClearsErrorAndNotifiesFlag()
        {
            var viewModel = CreateValidViewModel();
            string validEmail = viewModel.Email;
            viewModel.Email = string.Empty;
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            await AssertInvalidRegistration(viewModel);
            viewModel.Email = validEmail;
            await viewModel.RegisterCommand.ExecuteAsync(null);

            viewModel.InvalidRegisterFormErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterFormInvalid.Should().BeFalse();
            changedProperties.Count(name => name == nameof(RegisterFlowViewModel.InvalidRegisterFormErrorMessage)).Should().Be(2);
            changedProperties.Count(name => name == nameof(RegisterFlowViewModel.IsRegisterFormInvalid)).Should().Be(2);
        }

        [Fact]
        public void PasswordMismatch_BeforeConfirmationWasLeft_DoesNotShowError()
        {
            var viewModel = CreateValidViewModel();

            viewModel.ConfirmPassword = "Different12!";

            viewModel.RegisterPasswordMismatchErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterPasswordMismatch.Should().BeFalse();

            viewModel.ValidatePasswordConfirmationCommand.Execute(null);

            viewModel.RegisterPasswordMismatchErrorMessage.Should().Be(AppResources.Register_ErrorMessage_PasswordMismatch);
            viewModel.IsRegisterPasswordMismatch.Should().BeTrue();
        }

        [Fact]
        public void PasswordMismatch_AfterReset_WaitsForConfirmationToBeLeftAgain()
        {
            var viewModel = CreateValidViewModel();
            viewModel.ConfirmPassword = "Different12!";
            viewModel.ValidatePasswordConfirmationCommand.Execute(null);
            viewModel.IsRegisterPasswordMismatch.Should().BeTrue();

            viewModel.Reset();
            viewModel.RegisterPassword = "Secret12!";
            viewModel.ConfirmPassword = "Different12!";

            viewModel.RegisterPasswordMismatchErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterPasswordMismatch.Should().BeFalse();

            viewModel.ValidatePasswordConfirmationCommand.Execute(null);

            viewModel.IsRegisterPasswordMismatch.Should().BeTrue();
        }

        private static RegisterFlowViewModel CreateValidViewModel()
        {
            var response = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
            var session = new FakeAuthSession
            {
                RegisterHandler = (_, _) => Task.FromResult(new JassTournamentManager.App.Infrastructure.Auth.LoginResult(response, JassTournamentManager.App.Infrastructure.Auth.AuthFailure.None))
            };

            return new RegisterFlowViewModel(session, Array.Empty<ClaimableUserListItem>())
            {
                FirstName = "Alex",
                LastName = "River",
                Email = "player@example.org",
                RegisterPassword = "Secret12!",
                ConfirmPassword = "Secret12!"
            };
        }

        private static void SetName(RegisterFlowViewModel viewModel, bool firstName, string value)
        {
            if (firstName)
            {
                viewModel.FirstName = value;
            }
            else
            {
                viewModel.LastName = value;
            }
        }

        private static async Task AssertRegistrationResult(RegisterFlowViewModel viewModel, bool valid)
        {
            if (!valid)
            {
                await AssertInvalidRegistration(viewModel);
                return;
            }

            viewModel.InvalidRegisterFormErrorMessage = AppResources.Register_ErrorMessage_RegisterFormInvalid;

            await viewModel.RegisterCommand.ExecuteAsync(null);

            viewModel.InvalidRegisterFormErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterFormInvalid.Should().BeFalse();
        }

        private static async Task AssertInvalidRegistration(RegisterFlowViewModel viewModel)
        {
            await viewModel.RegisterCommand.ExecuteAsync(null);

            viewModel.InvalidRegisterFormErrorMessage.Should().Be(AppResources.Register_ErrorMessage_RegisterFormInvalid);
            viewModel.IsRegisterFormInvalid.Should().BeTrue();
        }
    }
}