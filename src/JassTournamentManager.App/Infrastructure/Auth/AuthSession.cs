using CommunityToolkit.Mvvm.ComponentModel;
using JassTournamentManager.Contracts.Auth;
using JassTournamentManager.Contracts.Users;

namespace JassTournamentManager.App.Infrastructure.Auth
{
    public sealed partial class AuthSession : ObservableObject, IAuthSession
    {
        private readonly IAuthApiClient apiClient;
        private readonly ITokenStore tokenStore;

        [ObservableProperty]
        public partial UserResponse? CurrentUser { get; private set; }

        [ObservableProperty]
        public partial bool LogoutWarning { get; private set; }

        public AuthSession(IAuthApiClient apiClient, ITokenStore tokenStore)
        {
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            this.tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            LoginResult result = await apiClient.LoginAsync(request, cancellationToken);
            if (result.Session is null)
            {
                return result;
            }

            try
            {
                await tokenStore.SetTokensAsync(result.Session, cancellationToken);
            }
            catch (Exception exception)
            {
                // Remove any partially persisted session before reporting the storage failure.
                await ClearTokensAsync();
                if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                throw new AuthStorageException(exception);
            }

            LogoutWarning = false;
            CurrentUser = result.Session.User;
            return result;
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            bool revoked = false;
            try
            {
                StoredAuthTokens? tokens = await ReadTokensAsync(cancellationToken);
                revoked = tokens is null || await apiClient.LogoutAsync(tokens, cancellationToken);
            }
            finally
            {
                // Local logout must also work when the network is unavailable or cancellation occurs.
                await ClearTokensAsync();
                LogoutWarning = !revoked;
                CurrentUser = null;
            }
        }

        private async Task ClearTokensAsync()
        {
            try
            {
                await tokenStore.ClearAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                throw new AuthStorageException(exception);
            }
        }

        private async Task<StoredAuthTokens?> ReadTokensAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await tokenStore.GetTokensAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new AuthStorageException(exception);
            }
        }
    }
}