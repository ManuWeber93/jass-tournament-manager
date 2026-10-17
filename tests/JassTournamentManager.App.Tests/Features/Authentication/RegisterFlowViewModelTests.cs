using FluentAssertions;
using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Features.Authentication.Models;
using Xunit;

namespace JassTournamentManager.App.Tests.Features.Authentication
{
    public class RegisterFlowViewModelTests
    {
        [Fact]
        public void Constructor_ByDefault_InitializesEmptyFormAndQuestionStep()
        {
            var viewModel = new RegisterFlowViewModel();

            viewModel.CurrentRegisterStep.Should().Be(RegisterStep.PreviousParticipationQuestion);
            viewModel.IsPreviousParticipationQuestionVisible.Should().BeTrue();
            viewModel.IsClaimableUserSelectionVisible.Should().BeFalse();
            viewModel.IsRegistrationFormVisible.Should().BeFalse();
            viewModel.Email.Should().BeEmpty();
            viewModel.FirstName.Should().BeEmpty();
            viewModel.LastName.Should().BeEmpty();
            viewModel.RegisterPassword.Should().BeEmpty();
            viewModel.ConfirmPassword.Should().BeEmpty();
            viewModel.ClaimableUsersFilterText.Should().BeEmpty();
            viewModel.SelectedClaimableUser.Should().BeNull();
            viewModel.RegisterPasswordMismatchErrorMessage.Should().BeEmpty();
            viewModel.InvalidRegisterFormErrorMessage.Should().BeEmpty();
            viewModel.IsRegisterPasswordMismatch.Should().BeFalse();
            viewModel.IsRegisterFormInvalid.Should().BeFalse();
            viewModel.ContinueWithSelectedClaimableUserCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithProvidedUsers_SortsByDisplayName()
        {
            var users = CreateUsers();

            var viewModel = new RegisterFlowViewModel(users);

            viewModel.FilteredClaimableUsers.Should().Equal(users.OrderBy(user => user.DisplayName));
        }

        [Fact]
        public void Constructor_WithEmptyUsers_InitializesEmptyList()
        {
            var viewModel = CreateViewModel();

            viewModel.FilteredClaimableUsers.Should().BeEmpty();
        }

        [Theory]
        [InlineData("  aLiCe  ", 1)]
        [InlineData("RIVER", 2)]
        [InlineData("ce Ri", 1)]
        [InlineData("not present", 0)]
        [InlineData("   ", 3)]
        [InlineData("", 3)]
        public void FilterText_WithSearchText_FiltersAndPreservesOrdering(string filter, int expectedCount)
        {
            var users = CreateUsers();
            var viewModel = new RegisterFlowViewModel(users);
            var collection = viewModel.FilteredClaimableUsers;
            var expectedUsers = users
                .Where(user => user.DisplayName.Contains(filter.Trim(), StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(user => user.DisplayName)
                .ToArray();

            viewModel.ClaimableUsersFilterText = filter;

            expectedUsers.Should().HaveCount(expectedCount);
            viewModel.FilteredClaimableUsers.Should().BeSameAs(collection);
            viewModel.FilteredClaimableUsers.Should().Equal(expectedUsers);
        }

        [Fact]
        public void FilterText_WhenCleared_RestoresAllArrangedUsers()
        {
            var users = CreateUsers();
            var viewModel = new RegisterFlowViewModel(users);
            viewModel.ClaimableUsersFilterText = users[0].FirstName;

            viewModel.ClaimableUsersFilterText = string.Empty;

            viewModel.FilteredClaimableUsers.Should().Equal(users.OrderBy(user => user.DisplayName));
        }

        [Fact]
        public void FilterText_WhenChanged_ClearsEvenMatchingSelectionAndDisablesContinue()
        {
            var user = CreateUsers()[0];
            var viewModel = new RegisterFlowViewModel(new[] { user });
            viewModel.SelectedClaimableUser = user;
            var changedProperties = new List<string?>();
            var canExecuteChangedCount = 0;
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);
            viewModel.ContinueWithSelectedClaimableUserCommand.CanExecuteChanged += (_, _) => canExecuteChangedCount++;

            viewModel.ClaimableUsersFilterText = user.FirstName;

            viewModel.FilteredClaimableUsers.Should().ContainSingle().Which.Should().BeSameAs(user);
            viewModel.SelectedClaimableUser.Should().BeNull();
            viewModel.ContinueWithSelectedClaimableUserCommand.CanExecute(null).Should().BeFalse();
            canExecuteChangedCount.Should().Be(1);
            changedProperties.Should().Contain(nameof(RegisterFlowViewModel.SelectedClaimableUser));
            changedProperties.Should().Contain(nameof(RegisterFlowViewModel.ClaimableUsersFilterText));
        }

        [Fact]
        public void SelectedClaimableUser_WhenChanged_UpdatesCommandAndNotifiesOnlyOncePerChange()
        {
            var viewModel = CreateViewModel();
            var user = CreateUsers()[0];
            var canExecuteStates = new List<bool>();
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);
            viewModel.ContinueWithSelectedClaimableUserCommand.CanExecuteChanged += (_, _) =>
                canExecuteStates.Add(viewModel.ContinueWithSelectedClaimableUserCommand.CanExecute(null));

            viewModel.SelectedClaimableUser = user;
            viewModel.SelectedClaimableUser = user;
            viewModel.SelectedClaimableUser = null;

            canExecuteStates.Should().Equal(true, false);
            changedProperties.Should().Equal(
                nameof(RegisterFlowViewModel.SelectedClaimableUser),
                nameof(RegisterFlowViewModel.SelectedClaimableUser));
        }

        [Theory]
        [InlineData(RegisterStep.PreviousParticipationQuestion)]
        [InlineData(RegisterStep.ClaimableUserSelection)]
        [InlineData(RegisterStep.RegistrationForm)]
        public void CurrentRegisterStep_WhenSet_ExposesOnlyCorrespondingPage(RegisterStep step)
        {
            var viewModel = CreateViewModel();

            viewModel.CurrentRegisterStep = step;

            viewModel.IsPreviousParticipationQuestionVisible.Should().Be(step == RegisterStep.PreviousParticipationQuestion);
            viewModel.IsClaimableUserSelectionVisible.Should().Be(step == RegisterStep.ClaimableUserSelection);
            viewModel.IsRegistrationFormVisible.Should().Be(step == RegisterStep.RegistrationForm);
        }

        [Fact]
        public void CurrentRegisterStep_WhenChanged_NotifiesAllVisibilityPropertiesOnlyOnce()
        {
            var viewModel = CreateViewModel();
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            viewModel.CurrentRegisterStep = RegisterStep.ClaimableUserSelection;
            viewModel.CurrentRegisterStep = RegisterStep.ClaimableUserSelection;

            changedProperties.Should().BeEquivalentTo(new[]
            {
                nameof(RegisterFlowViewModel.CurrentRegisterStep),
                nameof(RegisterFlowViewModel.IsPreviousParticipationQuestionVisible),
                nameof(RegisterFlowViewModel.IsClaimableUserSelectionVisible),
                nameof(RegisterFlowViewModel.IsRegistrationFormVisible)
            });
        }

        [Fact]
        public void ShowClaimableUserSelectionCommand_WhenExecuted_ShowsSelection()
        {
            var viewModel = CreateViewModel();

            viewModel.ShowClaimableUserSelectionCommand.Execute(null);

            viewModel.CurrentRegisterStep.Should().Be(RegisterStep.ClaimableUserSelection);
        }

        [Fact]
        public void ContinueWithSelectedClaimableUserCommand_WithSelection_PrefillsNamesAndShowsForm()
        {
            var user = CreateUsers()[0];
            var viewModel = new RegisterFlowViewModel(new[] { user });
            viewModel.SelectedClaimableUser = user;
            viewModel.FirstName = "Previous";
            viewModel.LastName = "Values";
            const string email = "player@example.org";
            viewModel.Email = email;

            viewModel.ContinueWithSelectedClaimableUserCommand.Execute(null);

            viewModel.FirstName.Should().Be(user.FirstName);
            viewModel.LastName.Should().Be(user.LastName);
            viewModel.Email.Should().Be(email);
            viewModel.SelectedClaimableUser.Should().BeSameAs(user);
            viewModel.CurrentRegisterStep.Should().Be(RegisterStep.RegistrationForm);
        }

        [Fact]
        public void ContinueWithSelectedClaimableUserCommand_WithoutSelection_DoesNotNavigateOrPrefill()
        {
            var viewModel = CreateViewModel();
            viewModel.CurrentRegisterStep = RegisterStep.ClaimableUserSelection;

            viewModel.ContinueWithSelectedClaimableUserCommand.Execute(null);

            viewModel.CurrentRegisterStep.Should().Be(RegisterStep.ClaimableUserSelection);
            viewModel.FirstName.Should().BeEmpty();
            viewModel.LastName.Should().BeEmpty();
        }

        [Fact]
        public void NoUserClaimingCommand_WithSelection_ClearsSelectionAndShowsForm()
        {
            var viewModel = CreateViewModel();
            viewModel.SelectedClaimableUser = CreateUsers()[0];

            viewModel.NoUserClaimingCommand.Execute(null);

            viewModel.SelectedClaimableUser.Should().BeNull();
            viewModel.ContinueWithSelectedClaimableUserCommand.CanExecute(null).Should().BeFalse();
            viewModel.CurrentRegisterStep.Should().Be(RegisterStep.RegistrationForm);
        }

        [Theory]
        [InlineData(RegisterStep.PreviousParticipationQuestion, false, RegisterStep.PreviousParticipationQuestion)]
        [InlineData(RegisterStep.ClaimableUserSelection, false, RegisterStep.PreviousParticipationQuestion)]
        [InlineData(RegisterStep.RegistrationForm, false, RegisterStep.PreviousParticipationQuestion)]
        [InlineData(RegisterStep.RegistrationForm, true, RegisterStep.ClaimableUserSelection)]
        public void RegisterNavigateBackCommand_WithCurrentState_ShowsExpectedStep(
            RegisterStep currentStep, bool hasSelection, RegisterStep expectedStep)
        {
            var viewModel = CreateViewModel();
            viewModel.CurrentRegisterStep = currentStep;
            var selectedUser = hasSelection ? CreateUsers()[0] : null;
            viewModel.SelectedClaimableUser = selectedUser;

            viewModel.RegisterNavigateBackCommand.Execute(null);

            viewModel.CurrentRegisterStep.Should().Be(expectedStep);
            viewModel.SelectedClaimableUser.Should().BeSameAs(selectedUser);
        }

        private static RegisterFlowViewModel CreateViewModel()
        {
            return new(Array.Empty<ClaimableUserListItem>());
        }

        private static ClaimableUserListItem[] CreateUsers()
        {
            return new[]
            {
                new ClaimableUserListItem(Guid.NewGuid(), "Zoe", "River"),
                new ClaimableUserListItem(Guid.NewGuid(), "Alice", "River"),
                new ClaimableUserListItem(Guid.NewGuid(), "Brian", "Stone")
            };
        }
    }
}