using JassTournamentManager.Contracts.TournamentConfigs;
using JassTournamentManager.Domain.Enums;

namespace JassTournamentManager.Contracts.Tournaments
{
    public sealed record TournamentResponse(
        Guid Id,
        Guid OrganizerId,
        string Name,
        string? Location,
        DateOnly Date,
        string TournamentCode,
        TournamentConfigDto Config,
        TournamentStatus Status,
        IReadOnlyList<RoundResponse> Rounds,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}