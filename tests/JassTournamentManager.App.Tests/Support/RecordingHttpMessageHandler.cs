namespace JassTournamentManager.App.Tests.Support;

internal sealed class RecordingHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = new();
    public List<CancellationToken> CancellationTokens { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        CancellationTokens.Add(cancellationToken);
        return respond(request, cancellationToken);
    }
}