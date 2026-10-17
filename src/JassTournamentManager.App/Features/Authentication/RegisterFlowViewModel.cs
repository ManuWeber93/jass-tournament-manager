using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JassTournamentManager.App.Features.Authentication.Models;
using JassTournamentManager.App.Resources.Localization;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace JassTournamentManager.App.Features.Authentication
{
    public partial class RegisterFlowViewModel : ObservableObject
    {
        private readonly List<ClaimableUserListItem> allClaimableUsers = new();

        private readonly Regex PasswordRegex = new(PasswordPattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));

        public const string PasswordPattern = @"^(?=.*\d)(?=.*[^\w\s]).{8,}$";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPreviousParticipationQuestionVisible))]
        [NotifyPropertyChangedFor(nameof(IsClaimableUserSelectionVisible))]
        [NotifyPropertyChangedFor(nameof(IsRegistrationFormVisible))]
        public partial RegisterStep CurrentRegisterStep { get; set; } = RegisterStep.PreviousParticipationQuestion;

        [ObservableProperty]
        public partial string Email { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string FirstName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string LastName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string RegisterPassword { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ConfirmPassword { get; set; } = string.Empty;

        public ObservableCollection<ClaimableUserListItem> FilteredClaimableUsers { get; } = new();

        [ObservableProperty]
        public partial string ClaimableUsersFilterText { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ContinueWithSelectedClaimableUserCommand))]
        public partial ClaimableUserListItem? SelectedClaimableUser { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRegisterPasswordMismatch))]
        public partial string RegisterPasswordMismatchErrorMessage { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRegisterFormInvalid))]
        public partial string InvalidRegisterFormErrorMessage { get; set; } = string.Empty;

        public RegisterFlowViewModel() : this(CreateDummyClaimableUsers())
        {
        }

        public RegisterFlowViewModel(IEnumerable<ClaimableUserListItem> claimableUsers)
        {
            ArgumentNullException.ThrowIfNull(claimableUsers);
            allClaimableUsers.AddRange(claimableUsers);
            ApplyClaimableUserFilter();
        }

        public bool IsPreviousParticipationQuestionVisible => CurrentRegisterStep == RegisterStep.PreviousParticipationQuestion;

        public bool IsClaimableUserSelectionVisible => CurrentRegisterStep == RegisterStep.ClaimableUserSelection;

        public bool IsRegistrationFormVisible => CurrentRegisterStep == RegisterStep.RegistrationForm;

        public bool IsRegisterPasswordMismatch => RegisterPasswordMismatchErrorMessage != string.Empty;

        public bool IsRegisterFormInvalid => InvalidRegisterFormErrorMessage != string.Empty;

        partial void OnClaimableUsersFilterTextChanged(string value)
        {
            SelectedClaimableUser = null;
            ApplyClaimableUserFilter();
        }

        partial void OnRegisterPasswordChanged(string value)
        {
            ValidateIfPasswordsMatch();
        }

        partial void OnConfirmPasswordChanged(string value)
        {
            ValidateIfPasswordsMatch();
        }

        [RelayCommand]
        private void Register()
        {
            if (!IsRegisterFormValid())
            {
                InvalidRegisterFormErrorMessage = AppResources.Register_ErrorMessage_RegisterFormInvalid;
                return;
            }

            InvalidRegisterFormErrorMessage = string.Empty;

            // Later: call register endpoint.

            // Show error message if backend sends error back (e.g. duplicate email)
        }

        [RelayCommand]
        private void RegisterNavigateBack()
        {
            if (CurrentRegisterStep == RegisterStep.ClaimableUserSelection)
            {
                CurrentRegisterStep = RegisterStep.PreviousParticipationQuestion;
            }

            if (CurrentRegisterStep == RegisterStep.RegistrationForm)
            {
                if (SelectedClaimableUser is not null)
                {
                    CurrentRegisterStep = RegisterStep.ClaimableUserSelection;
                }
                else
                {
                    CurrentRegisterStep = RegisterStep.PreviousParticipationQuestion;
                }
            }
        }

        [RelayCommand]
        private void ShowClaimableUserSelection()
        {
            CurrentRegisterStep = RegisterStep.ClaimableUserSelection;
        }

        [RelayCommand]
        private void NoUserClaiming()
        {
            SelectedClaimableUser = null;
            CurrentRegisterStep = RegisterStep.RegistrationForm;
        }

        [RelayCommand(CanExecute = nameof(CanContinueWithSelectedClaimableUser))]
        private void ContinueWithSelectedClaimableUser()
        {
            if (SelectedClaimableUser is null)
            {
                return;
            }

            PrefillRegistrationForm(SelectedClaimableUser);
            CurrentRegisterStep = RegisterStep.RegistrationForm;
        }

        private static IEnumerable<ClaimableUserListItem> CreateDummyClaimableUsers()
        {
            return
            [
                new(Guid.NewGuid(), "Max", "Muster"),
                new(Guid.NewGuid(), "Maja", "Meisterhans"),
                new(Guid.NewGuid(), "Seraina", "Huber")
            ];
        }

        private void ValidateIfPasswordsMatch()
        {
            if (RegisterPassword == string.Empty || ConfirmPassword == string.Empty)
            {
                RegisterPasswordMismatchErrorMessage = string.Empty;
                return;
            }

            if (RegisterPassword == ConfirmPassword)
            {
                RegisterPasswordMismatchErrorMessage = string.Empty;
                return;
            }

            RegisterPasswordMismatchErrorMessage = AppResources.Register_ErrorMessage_PasswordMismatch;
        }

        private void ApplyClaimableUserFilter()
        {
            string filterValue = ClaimableUsersFilterText.Trim();

            IEnumerable<ClaimableUserListItem> filteredUsers = allClaimableUsers.OrderBy(user => user.DisplayName);
            if (!string.IsNullOrWhiteSpace(filterValue))
            {
                filteredUsers = filteredUsers.Where(user =>
                    user.DisplayName.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase));
            }

            FilteredClaimableUsers.Clear();

            foreach (ClaimableUserListItem user in filteredUsers)
            {
                FilteredClaimableUsers.Add(user);
            }
        }

        private bool CanContinueWithSelectedClaimableUser()
        {
            return SelectedClaimableUser is not null;
        }

        private void PrefillRegistrationForm(ClaimableUserListItem selectedClaimableUser)
        {
            FirstName = selectedClaimableUser.FirstName;
            LastName = selectedClaimableUser.LastName;
        }

        private bool IsRegisterFormValid()
        {
            if (string.IsNullOrWhiteSpace(FirstName) || FirstName.Length > 50)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastName) || LastName.Length > 50)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(Email) || Email.Length > 320 || !IsValidEmailAddress(Email))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(RegisterPassword) || !PasswordRegex.IsMatch(RegisterPassword))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword) || ConfirmPassword != RegisterPassword)
            {
                return false;
            }

            return true;
        }

        private static bool IsValidEmailAddress(string email)
        {
            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(email);
                return string.Equals(mailAddress.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
