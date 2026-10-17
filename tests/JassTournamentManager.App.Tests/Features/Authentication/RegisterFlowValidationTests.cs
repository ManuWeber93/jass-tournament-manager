using FluentAssertions;
using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Features.Authentication.Models;
using JassTournamentManager.App.Resources.Localization;
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

            viewModel.RegisterPassword = password;
            viewModel.ConfirmPassword = confirmation;

            viewModel.RegisterPasswordMismatchErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterPasswordMismatch.Should().BeFalse();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Passwords_WhenEitherValueChangesToMismatch_ShowResourceMessage(bool changePassword)
        {
            var viewModel = CreateValidViewModel();

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
            viewModel.ConfirmPassword = "Different12!";

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
        public void RegisterCommand_WithBlankName_ShowsInvalidFormResource(bool firstName, string value)
        {
            var viewModel = CreateValidViewModel();
            SetName(viewModel, firstName, value);

            AssertInvalidRegistration(viewModel);
        }

        [Theory]
        [InlineData(true, 1, true)]
        [InlineData(true, 50, true)]
        [InlineData(true, 51, false)]
        [InlineData(false, 1, true)]
        [InlineData(false, 50, true)]
        [InlineData(false, 51, false)]
        public void RegisterCommand_WithNameLength_ValidatesBoundary(bool firstName, int length, bool valid)
        {
            var viewModel = CreateValidViewModel();
            SetName(viewModel, firstName, new string('a', length));

            AssertRegistrationResult(viewModel, valid);
        }

        [Theory]
        [InlineData(319, true)]
        [InlineData(320, true)]
        [InlineData(321, false)]
        public void RegisterCommand_WithEmailLength_ValidatesBoundary(int length, bool valid)
        {
            var viewModel = CreateValidViewModel();
            const string suffix = "@example.org";
            viewModel.Email = new string('a', length - suffix.Length) + suffix;

            AssertRegistrationResult(viewModel, valid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-an-email")]
        [InlineData("player@")]
        [InlineData("@example.org")]
        [InlineData("Player <player@example.org>")]
        [InlineData(" player@example.org ")]
        public void RegisterCommand_WithInvalidEmail_ShowsInvalidFormResource(string email)
        {
            var viewModel = CreateValidViewModel();
            viewModel.Email = email;

            AssertInvalidRegistration(viewModel);
        }

        [Theory]
        [InlineData("player+event@example.org")]
        [InlineData("PLAYER@EXAMPLE.ORG")]
        [InlineData("first.last@example.org")]
        public void RegisterCommand_WithValidEmail_ClearsPreviousError(string email)
        {
            var viewModel = CreateValidViewModel();
            viewModel.Email = email;

            AssertRegistrationResult(viewModel, true);
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
        public void RegisterCommand_WithPassword_ValidatesLengthDigitAndSpecialCharacter(string password, bool valid)
        {
            var viewModel = CreateValidViewModel();
            viewModel.RegisterPassword = password;
            viewModel.ConfirmPassword = password;

            AssertRegistrationResult(viewModel, valid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Different12!")]
        [InlineData("secret12!")]
        public void RegisterCommand_WithMissingOrDifferentConfirmation_ShowsInvalidFormResource(string confirmation)
        {
            var viewModel = CreateValidViewModel();
            viewModel.ConfirmPassword = confirmation;

            AssertInvalidRegistration(viewModel);
        }

        [Fact]
        public void RegisterCommand_AfterCorrectingInvalidForm_ClearsErrorAndNotifiesFlag()
        {
            var viewModel = CreateValidViewModel();
            string validEmail = viewModel.Email;
            viewModel.Email = string.Empty;
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            AssertInvalidRegistration(viewModel);
            viewModel.Email = validEmail;
            viewModel.RegisterCommand.Execute(null);

            viewModel.InvalidRegisterFormErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterFormInvalid.Should().BeFalse();
            changedProperties.Count(name => name == nameof(RegisterFlowViewModel.InvalidRegisterFormErrorMessage)).Should().Be(2);
            changedProperties.Count(name => name == nameof(RegisterFlowViewModel.IsRegisterFormInvalid)).Should().Be(2);
        }

        private static RegisterFlowViewModel CreateValidViewModel()
        {
            return new RegisterFlowViewModel(Array.Empty<ClaimableUserListItem>())
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

        private static void AssertRegistrationResult(RegisterFlowViewModel viewModel, bool valid)
        {
            if (!valid)
            {
                AssertInvalidRegistration(viewModel);
                return;
            }

            viewModel.InvalidRegisterFormErrorMessage = AppResources.Register_ErrorMessage_RegisterFormInvalid;

            viewModel.RegisterCommand.Execute(null);

            viewModel.InvalidRegisterFormErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterFormInvalid.Should().BeFalse();
        }

        private static void AssertInvalidRegistration(RegisterFlowViewModel viewModel)
        {
            viewModel.RegisterCommand.Execute(null);

            viewModel.InvalidRegisterFormErrorMessage.Should().Be(AppResources.Register_ErrorMessage_RegisterFormInvalid);
            viewModel.IsRegisterFormInvalid.Should().BeTrue();
        }
    }
}