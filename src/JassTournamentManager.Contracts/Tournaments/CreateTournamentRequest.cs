using JassTournamentManager.Contracts.TournamentConfigs;

namespace JassTournamentManager.Contracts.Tournaments
{
    public sealed record CreateTournamentRequest(
        string Name,
        string? Location,
        DateOnly Date,
        TournamentConfigDto Config);
}