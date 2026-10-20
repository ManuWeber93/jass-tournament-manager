using JassTournamentManager.Domain.Services;

namespace JassTournamentManager.Application.Tournaments
{
    public sealed class RandomTournamentCodeGenerator : ITournamentCodeGenerator
    {
        public string Generate() => TournamentCodeGenerator.GenerateTournamentCode();
    }
}