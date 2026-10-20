using JassTournamentManager.Application.Common;

namespace JassTournamentManager.Application.Tournaments
{
    public static class TournamentErrors
    {
        public static readonly Error OrganizerNotFound = new("Tournaments.OrganizerNotFound", "Organizer not found.", ErrorType.NotFound);
        public static readonly Error NotFound = new("Tournaments.NotFound", "No tournament with the given id found.", ErrorType.NotFound);
        public static readonly Error CodeGenerationFailed = new("Tournaments.CodeGenerationFailed", "Could not allocate a unique tournament code. Please retry.", ErrorType.Conflict);
    }
}