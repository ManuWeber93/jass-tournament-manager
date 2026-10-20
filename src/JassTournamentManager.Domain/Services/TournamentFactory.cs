using JassTournamentManager.Domain.Entities;
using JassTournamentManager.Domain.Enums;
using JassTournamentManager.Domain.ValueObjects;

namespace JassTournamentManager.Domain.Services
{
    public static class TournamentFactory
    {
        public static Tournament Create(
            Guid organizerId,
            TournamentDetails details,
            TournamentConfigValues configValues)
        {
            ArgumentNullException.ThrowIfNull(details);
            ArgumentNullException.ThrowIfNull(configValues);

            var numberOfRounds = configValues.NumberOfRounds;
            var tournament = new Tournament(organizerId, details, configValues);

            tournament.AddParticipant(new TournamentParticipant(
                tournament.Id, organizerId, RegistrationMethod.ByOrganizer, ParticipantRole.Organizer, isPlaying: true));

            for (var roundNumber = 1; roundNumber <= numberOfRounds; roundNumber++)
            {
                tournament.AddRound(new Round(tournament.Id, roundNumber, RoundStatus.Pending));
            }

            return tournament;
        }
    }
}