namespace JassTournamentManager.Application.Tournaments
{
    public sealed class TournamentCodeConflictException : Exception
    {
        public TournamentCodeConflictException(Exception innerException)
            : base("The tournament code is already in use.", innerException)
        {
        }
    }
}