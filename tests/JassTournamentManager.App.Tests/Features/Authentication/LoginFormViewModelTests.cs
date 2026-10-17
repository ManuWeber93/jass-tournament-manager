using FluentAssertions;
using JassTournamentManager.App.Features.Authentication;
using Xunit;

namespace JassTournamentManager.App.Tests.Features.Authentication
{
    public class LoginFormViewModelTests
    {
        [Fact]
        public void Constructor_ByDefault_InitializesEmptyCredentials()
        {
            var viewModel = new LoginFormViewModel();

            viewModel.Email.Should().BeEmpty();
            viewModel.Password.Should().BeEmpty();
        }

        [Theory]
        [InlineData(nameof(LoginFormViewModel.Email), "player@example.org")]
        [InlineData(nameof(LoginFormViewModel.Password), "Secret123!")]
        public void Credential_WhenChanged_StoresValueAndNotifiesOnlyOnce(string propertyName, string value)
        {
            var viewModel = new LoginFormViewModel();
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            if (propertyName == nameof(LoginFormViewModel.Email))
            {
                viewModel.Email = value;
                viewModel.Email = value;
                viewModel.Email.Should().Be(value);
                viewModel.Password.Should().BeEmpty();
            }
            else
            {
                viewModel.Password = value;
                viewModel.Password = value;
                viewModel.Password.Should().Be(value);
                viewModel.Email.Should().BeEmpty();
            }

            changedProperties.Should().Equal(propertyName);
        }
    }
}