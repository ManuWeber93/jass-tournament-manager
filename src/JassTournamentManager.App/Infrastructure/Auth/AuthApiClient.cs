using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JassTournamentManager.Contracts.Auth;

namespace JassTournamentManager.App.Infrastructure.Auth
{
    /// <summary>
    /// Uses a dedicated anonymous HTTP client without an authentication or refresh handler.
    /// </summary>
    public sealed class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient httpClient;

        public AuthApiClient(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                return await ExecuteLoginAsync(request, cancellationToken);
            }
            catch (JsonException)
            {
                return new LoginResult(null, AuthFailure.InvalidResponse);
            }
            catch (HttpRequestException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new LoginResult(null, AuthFailure.Network);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new LoginResult(null, AuthFailure.Network);
            }
        }

        private async Task<LoginResult> ExecuteLoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                AuthFailure failure = response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized
                    ? AuthFailure.InvalidCredentials
                    : AuthFailure.Server;

                return new LoginResult(null, failure);
            }

            AuthResponse? session = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);

            return IsValidSession(session)
                ? LoginResult.Success(session!)
                : new LoginResult(null, AuthFailure.InvalidResponse);
        }

        public async Task<bool> LogoutAsync(StoredAuthTokens tokens, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(tokens);

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout")
            {
                Content = JsonContent.Create(new LogoutRequest(tokens.RefreshToken))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            try
            {
                // Log out with the supplied tokens without creating a new session through token refresh.
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        private static bool IsValidSession(AuthResponse? session) =>
            session is not null
            && !string.IsNullOrWhiteSpace(session.AccessToken)
            && !string.IsNullOrWhiteSpace(session.RefreshToken)
            && session.AccessTokenExpiresAt != default
            && session.RefreshTokenExpiresAt != default
            && session.User is not null
            && session.User.Id != Guid.Empty;
    }
}