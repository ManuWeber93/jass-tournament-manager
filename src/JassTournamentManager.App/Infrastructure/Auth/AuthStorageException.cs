namespace JassTournamentManager.App.Infrastructure.Auth
{
    public sealed class AuthStorageException : Exception
    {
        public AuthStorageException(Exception innerException)
            : base("Authentication tokens could not be persisted or removed.", innerException)
        {
        }
    }
}