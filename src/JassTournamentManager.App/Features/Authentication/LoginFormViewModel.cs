using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Resources.Localization;
using JassTournamentManager.Contracts.Auth;
using System.ComponentModel.DataAnnotations;

namespace JassTournamentManager.App.Features.Authentication
{
    public partial class LoginFormViewModel : ObservableObject
    {
        private readonly IAuthSession session;

        [ObservableProperty]
        public partial string Email { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Password { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        public partial string ErrorMessage { get; set; } = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        public partial bool IsBusy { get; private set; }

        public bool IsNotBusy => !IsBusy;

        public LoginFormViewModel(IAuthSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            session.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IAuthSession.CurrentUser) && session.CurrentUser is null)
                {
                    Password = string.Empty;
                    ErrorMessage = session.LogoutWarning ? AppResources.Auth_LogoutRemoteFailed : string.Empty;
                }
            };
        }

        private bool CanLogin() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync(CancellationToken cancellationToken)
        {
            if (IsBusy)
            {
                return;
            }

            ErrorMessage = string.Empty;
            var request = new LoginRequest(Email.Trim(), Password);
            if (!IsValidLoginRequest(request))
            {
                ErrorMessage = AppResources.Auth_InvalidInput;
                return;
            }

            await ExecuteLoginAsync(request, cancellationToken);
        }

        private static bool IsValidLoginRequest(LoginRequest request)
        {
            return !string.IsNullOrWhiteSpace(request.Email)
                && request.Email.Length <= 320
                && new EmailAddressAttribute().IsValid(request.Email)
                && !string.IsNullOrWhiteSpace(request.Password);
        }

        private async Task ExecuteLoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;
                LoginResult result = await session.LoginAsync(request, cancellationToken);
                HandleLoginResult(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // A canceled login is not an authentication error.
            }
            catch (AuthStorageException)
            {
                ErrorMessage = AppResources.Auth_StorageError;
            }
            catch (Exception)
            {
                ErrorMessage = AppResources.Auth_UnexpectedError;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void HandleLoginResult(LoginResult result)
        {
            if (result.Session is not null)
            {
                Password = string.Empty;
                return;
            }

            SetErrorMessageBasedOnResult(result);
        }

        private void SetErrorMessageBasedOnResult(LoginResult result)
        {
            ErrorMessage = result.Failure switch
            {
                AuthFailure.InvalidCredentials => AppResources.Auth_InvalidCredentials,
                AuthFailure.Network => AppResources.Auth_NetworkError,
                AuthFailure.InvalidResponse => AppResources.Auth_InvalidResponse,
                _ => AppResources.Auth_ServerError
            };
        }
    }
}
