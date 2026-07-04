using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.Contracts.Auth;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace JassTournamentManager.App.Infrastructure.Api;

public sealed class AuthenticatingHttpHandler : DelegatingHandler
{
    private readonly ITokenStore tokenStore;

    public AuthenticatingHttpHandler(ITokenStore tokenStore)
    {
        this.tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (IsRefreshRequest(request))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        await AddAccessTokenAsync(request, cancellationToken);

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        HttpRequestMessage? retryRequest = await TryCreateRetryRequestAsync(request, cancellationToken);
        if (retryRequest is null)
        {
            return response;
        }

        response.Dispose();

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task AddAccessTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? accessToken = await tokenStore.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<HttpRequestMessage?> TryCreateRetryRequestAsync(HttpRequestMessage originalRequest, CancellationToken cancellationToken)
    {
        string? refreshToken = await tokenStore.GetRefreshTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        AuthResponse? refreshedSession = await TryRefreshSessionAsync(refreshToken, cancellationToken);
        if (refreshedSession is null)
        {
            await tokenStore.ClearAsync(cancellationToken);
            return null;
        }

        await tokenStore.SetTokensAsync(refreshedSession, cancellationToken);

        HttpRequestMessage retryRequest = await CloneRequestAsync(originalRequest, cancellationToken);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedSession.AccessToken);

        return retryRequest;
    }

    private async Task<AuthResponse?> TryRefreshSessionAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
        {
            Content = JsonContent.Create(new RefreshSessionRequest(refreshToken))
        };

        using HttpResponseMessage refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);
        if (!refreshResponse.IsSuccessStatusCode)
        {
            return null;
        }

        return await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
    }

    private static bool IsRefreshRequest(HttpRequestMessage request)
    {
        string requestPath = request.RequestUri?.OriginalString ?? string.Empty;
        return requestPath.Contains("api/auth/refresh", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (KeyValuePair<string, object?> option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}