using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JassTournamentManager.App.Features.Authentication.Models;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Resources.Localization;
using JassTournamentManager.Contracts.Auth;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace JassTournamentManager.App.Features.Authentication
{
    public partial class RegisterFlowViewModel : ObservableObject
    {
        private readonly IAuthSession session;

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        [NotifyCanExecuteChangedFor(nameof(RegisterNavigateBackCommand))]
        [NotifyCanExecuteChangedFor(nameof(ShowClaimableUserSelectionCommand))]
        [NotifyCanExecuteChangedFor(nameof(NoUserClaimingCommand))]
        [NotifyCanExecuteChangedFor(nameof(ContinueWithSelectedClaimableUserCommand))]
        public partial bool IsBusy { get; private set; }

        public bool IsNotBusy => !IsBusy;

        public RegisterFlowViewModel(IAuthSession session) : this(session, Array.Empty<ClaimableUserListItem>())
        {
        }

        public RegisterFlowViewModel(IAuthSession session, IEnumerable<ClaimableUserListItem> claimableUsers)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            ArgumentNullException.ThrowIfNull(claimableUsers);
            allClaimableUsers.AddRange(claimableUsers);
            ApplyClaimableUserFilter();
            session.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IAuthSession.CurrentUser) && session.CurrentUser is null)
                {
                    ClearPasswords();
                    InvalidRegisterFormErrorMessage = string.Empty;
                }
            };
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

        [RelayCommand(CanExecute = nameof(CanNavigate))]
        private async Task RegisterAsync(CancellationToken cancellationToken)
        {
            if (IsBusy)
            {
                return;
            }

            InvalidRegisterFormErrorMessage = string.Empty;
            if (!IsRegisterFormValid())
            {
                InvalidRegisterFormErrorMessage = AppResources.Register_ErrorMessage_RegisterFormInvalid;
                return;
            }

            if (SelectedClaimableUser is not null)
            {
                InvalidRegisterFormErrorMessage = AppResources.Register_UserClaimingUnavailable;
                return;
            }

            var request = new RegisterRequest(null, Email.Trim(), RegisterPassword, FirstName.Trim(), LastName.Trim());
            await ExecuteRegistrationAsync(request, cancellationToken);
        }

        private async Task ExecuteRegistrationAsync(RegisterRequest request, CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;
                LoginResult result = await session.RegisterAsync(request, cancellationToken);
                HandleRegistrationResult(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // A canceled request does not confirm whether the account was created.
            }
            catch (AuthStorageException)
            {
                ClearPasswords();
                InvalidRegisterFormErrorMessage = AppResources.Register_StorageError;
            }
            catch (Exception)
            {
                InvalidRegisterFormErrorMessage = AppResources.Auth_UnexpectedError;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void HandleRegistrationResult(LoginResult result)
        {
            if (result.Session is not null)
            {
                ClearPasswords();
                return;
            }

            SetErrorMessageBasedOnResult(result);
        }

        private void SetErrorMessageBasedOnResult(LoginResult result)
        {
            InvalidRegisterFormErrorMessage = result.Failure switch
            {
                AuthFailure.InvalidInput => AppResources.Register_ErrorMessage_RegisterFormInvalid,
                AuthFailure.EmailAlreadyInUse => AppResources.Register_EmailAlreadyInUse,
                AuthFailure.UserClaimingDisabled => AppResources.Register_UserClaimingUnavailable,
                AuthFailure.Network => AppResources.Register_NetworkError,
                AuthFailure.InvalidResponse => AppResources.Register_InvalidResponse,
                _ => AppResources.Auth_ServerError
            };
        }

        private void ClearPasswords()
        {
            RegisterPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private bool CanNavigate() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanNavigate))]
        private void RegisterNavigateBack()
        {
            if (IsBusy)
            {
                return;
            }

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

        [RelayCommand(CanExecute = nameof(CanNavigate))]
        private void ShowClaimableUserSelection()
        {
            if (IsBusy)
            {
                return;
            }

            CurrentRegisterStep = RegisterStep.ClaimableUserSelection;
        }

        [RelayCommand(CanExecute = nameof(CanNavigate))]
        private void NoUserClaiming()
        {
            if (IsBusy)
            {
                return;
            }

            SelectedClaimableUser = null;
            CurrentRegisterStep = RegisterStep.RegistrationForm;
        }

        [RelayCommand(CanExecute = nameof(CanContinueWithSelectedClaimableUser))]
        private void ContinueWithSelectedClaimableUser()
        {
            if (!CanContinueWithSelectedClaimableUser())
            {
                return;
            }

            PrefillRegistrationForm(SelectedClaimableUser!);
            CurrentRegisterStep = RegisterStep.RegistrationForm;
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
            return !IsBusy && SelectedClaimableUser is not null;
        }

        private void PrefillRegistrationForm(ClaimableUserListItem selectedClaimableUser)
        {
            FirstName = selectedClaimableUser.FirstName;
            LastName = selectedClaimableUser.LastName;
        }

        private bool IsRegisterFormValid()
        {
            if (string.IsNullOrWhiteSpace(FirstName) || FirstName.Trim().Length > 50)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastName) || LastName.Trim().Length > 50)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(Email) || Email.Trim().Length > 320 || !IsValidEmailAddress(Email.Trim()))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(RegisterPassword) || RegisterPassword.Length > 200 || !PasswordRegex.IsMatch(RegisterPassword))
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
