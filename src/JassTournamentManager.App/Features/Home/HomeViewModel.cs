using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Resources.Localization;

namespace JassTournamentManager.App.Features.Home
{
    public sealed partial class HomeViewModel : ObservableObject
    {
        private readonly IAuthSession session;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        public partial string ErrorMessage { get; set; } = string.Empty;

        public string DisplayName => session.CurrentUser is { } user ? $"{user.FirstName} {user.LastName}" : string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        [NotifyCanExecuteChangedFor(nameof(LogoutCommand))]
        public partial bool IsBusy { get; private set; }

        public bool IsNotBusy => !IsBusy;

        public HomeViewModel(IAuthSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            session.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IAuthSession.CurrentUser))
                {
                    ErrorMessage = string.Empty;
                    OnPropertyChanged(nameof(DisplayName));
                }
            };
        }

        private bool CanLogout() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanLogout))]
        private async Task LogoutAsync(CancellationToken cancellationToken)
        {
            if (IsBusy)
            {
                return;
            }

            ErrorMessage = string.Empty;
            try
            {
                IsBusy = true;
                await session.LogoutAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Local cleanup has already been attempted by the session.
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
    }
}