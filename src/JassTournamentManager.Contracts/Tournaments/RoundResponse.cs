using JassTournamentManager.Domain.Enums;

namespace JassTournamentManager.Contracts.Tournaments
{
    public sealed record RoundResponse(Guid Id, int RoundNumber, RoundStatus Status);
}