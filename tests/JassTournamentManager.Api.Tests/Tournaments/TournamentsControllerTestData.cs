using JassTournamentManager.Contracts.TournamentConfigs;
using JassTournamentManager.Contracts.Tournaments;
using JassTournamentManager.Domain.Enums;

namespace JassTournamentManager.Api.Tests.Tournaments
{
    internal static class TournamentsControllerTestData
    {
        public static CreateTournamentRequest CreateRequest() => new(
            "Summer tournament",
            "Club house",
            new DateOnly(2026, 7, 18),
            new TournamentConfigDto(5, 8, true, false, ScoreVisibility.HiddenDuringActiveTournament));

        public static TournamentResponse CreateResponse(CreateTournamentRequest request) => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            request.Name,
            request.Location,
            request.Date,
            "ABC123",
            request.Config,
            TournamentStatus.Active,
            [new RoundResponse(Guid.NewGuid(), 1, RoundStatus.Pending)],
            new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 2, 13, 0, 0, TimeSpan.FromHours(2)));
    }
}