using JassTournamentManager.Domain.Common;
using JassTournamentManager.Domain.Enums;
using JassTournamentManager.Domain.ValueObjects;

namespace JassTournamentManager.Domain.Entities
{
    public class Tournament : Common.BaseEntity
    {
        private const TournamentStatus DefaultTournamentStatus = TournamentStatus.Active;

        private readonly List<Round> _rounds = [];
        private readonly List<TournamentParticipant> _participants = [];

        public Guid OrganizerId { get; private set; }

        public TournamentDetails Details { get; private set; } = null!;

        public TournamentConfigValues ConfigValues { get; private set; } = null!;

        public TournamentStatus Status { get; private set; }

        public IReadOnlyCollection<Round> Rounds => _rounds.AsReadOnly();

        public IReadOnlyCollection<TournamentParticipant> Participants => _participants.AsReadOnly();

        private Tournament() { }

        public Tournament(
            Guid organizerId,
            TournamentDetails details,
            TournamentConfigValues configValues,
            TournamentStatus status = DefaultTournamentStatus)
        {
            Guard.AgainstEmptyGuid(organizerId, nameof(organizerId));
            ArgumentNullException.ThrowIfNull(details);
            ArgumentNullException.ThrowIfNull(configValues);

            OrganizerId = organizerId;
            Details = details;
            ConfigValues = configValues;
            Status = status;
        }

        public void AddRound(Round round)
        {
            ArgumentNullException.ThrowIfNull(round);

            if (round.TournamentId != Id)
            {
                throw new InvalidOperationException("Round belongs to a different tournament.");
            }

            if (_rounds.Contains(round))
            {
                throw new InvalidOperationException("Round is already part of the tournament.");
            }

            if (_rounds.Any(r => r.RoundNumber == round.RoundNumber))
            {
                throw new InvalidOperationException($"Round number {round.RoundNumber} already exists in the tournament.");
            }

            _rounds.Add(round);
            UpdateNumberOfRoundsInConfig();
            MarkAsUpdated();
        }

        public void RemoveRound(Guid roundId)
        {
            var roundToRemove = _rounds.FirstOrDefault(r => r.Id == roundId)
                ?? throw new ArgumentException($"Round with id {roundId} not found in tournament.");

            _rounds.Remove(roundToRemove);
            UpdateNumberOfRoundsInConfig();
            MarkAsUpdated();
        }

        public void AddParticipant(TournamentParticipant participant)
        {
            ArgumentNullException.ThrowIfNull(participant);

            if (participant.TournamentId != Id)
            {
                throw new InvalidOperationException("Participant belongs to a different tournament.");
            }

            if (_participants.Contains(participant))
            {
                throw new InvalidOperationException("Participant is already part of the tournament.");
            }

            if (_participants.Any(p => p.UserId == participant.UserId))
            {
                throw new InvalidOperationException("User is already registered for the tournament.");
            }

            _participants.Add(participant);
            MarkAsUpdated();
        }

        public void RemoveParticipant(Guid participantId)
        {
            var participantToRemove = _participants.FirstOrDefault(p => p.Id == participantId)
                ?? throw new ArgumentException($"Participant with id {participantId} not found in tournament.");

            _participants.Remove(participantToRemove);
            MarkAsUpdated();
        }

        public void UpdateDetails(TournamentDetails newDetails)
        {
            ArgumentNullException.ThrowIfNull(newDetails);

            Details = newDetails;
            MarkAsUpdated();
        }

        public void UpdateConfigValues(TournamentConfigValues newConfigValues)
        {
            ArgumentNullException.ThrowIfNull(newConfigValues);

            UpdateMatchBonusEnabledForGames(ConfigValues, newConfigValues);

            ConfigValues = newConfigValues;
            MarkAsUpdated();
        }

        public void Complete()
        {
            Status = TournamentStatus.Completed;
            MarkAsUpdated();
        }

        public void Cancel()
        {
            Status = TournamentStatus.Cancelled;
            MarkAsUpdated();
        }

        public void Reactivate()
        {
            Status = TournamentStatus.Active;
            MarkAsUpdated();
        }

        private void UpdateNumberOfRoundsInConfig()
        {
            ConfigValues = ConfigValues with
            {
                NumberOfRounds = _rounds.Count
            };
        }

        private void UpdateMatchBonusEnabledForGames(TournamentConfigValues currentConfigValues, TournamentConfigValues newConfigValues)
        {
            if (currentConfigValues.MatchBonusEnabled == newConfigValues.MatchBonusEnabled)
            {
                return;
            }

            foreach (var round in _rounds)
            {
                round.UpdateMatchBonusEnabledForGames(newConfigValues.MatchBonusEnabled);
            }
        }
    }
}
