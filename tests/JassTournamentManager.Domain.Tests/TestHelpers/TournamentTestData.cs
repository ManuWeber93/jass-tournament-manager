using JassTournamentManager.Domain.Entities;
using JassTournamentManager.Domain.Services;
using JassTournamentManager.Domain.ValueObjects;

namespace JassTournamentManager.Domain.Tests.TestHelpers
{
    internal static class TournamentTestData
    {
        public static Guid CreateTournamentId() => Guid.NewGuid();

        public static string CreateTournamentName() => "Test Tournament";

        public static string CreateLocation() => "Zeigerhüsli Adliswil";

        public static DateOnly CreateTournamentDate() => new(2026, 5, 13);

        public static string CreateTournamentCode() => TournamentCodeGenerator.GenerateTournamentCode();

        public static TournamentConfigValues CreateTournamentConfigValues() => new();

        public static TournamentDetails CreateTournamentDetails() =>
            new(
                CreateTournamentName(),
                CreateLocation(),
                CreateTournamentDate(),
                CreateTournamentCode());

        public static Tournament CreateTournament() =>
            new(
                UserTestData.CreateUserId(),
                CreateTournamentDetails(),
                CreateTournamentConfigValues());
    }
}
