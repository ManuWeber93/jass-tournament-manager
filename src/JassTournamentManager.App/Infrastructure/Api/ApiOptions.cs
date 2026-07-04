namespace JassTournamentManager.App.Infrastructure.Api;

public sealed class ApiOptions
{
    public const string HttpClientName = "Api";

    public Uri BaseAddress { get; init; } = new(GetDefaultBaseAddress());

    private static string GetDefaultBaseAddress()
    {
#if ANDROID
        return "http://10.0.2.2:5272";
#else
        return "http://localhost:5272";
#endif
    }
}