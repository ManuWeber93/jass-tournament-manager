namespace JassTournamentManager.App.Infrastructure.Auth
{
    public enum AuthFailure
    {
        None,
        InvalidCredentials,
        Network,
        Server,
        InvalidResponse
    }
}