using JassTournamentManager.Domain.Common;
using JassTournamentManager.Domain.Enums;

namespace JassTournamentManager.Domain.Entities
{
    public class Pairing : BaseEntity
    {
        private const PairingStatus DefaultPairingStatus = PairingStatus.Pending;
        private const int RequiredParticipants = 4;
        private const int RequiredParticipantsPerTeam = 2;

        private readonly List<Game> _games = [];
        private readonly List<PairingParticipant> _participants = [];

        public Guid RoundId { get; private set; }

        public Guid JassTableId { get; private set; }

        public int GamesPerRound { get; private set; }

        public PairingStatus Status { get; private set; }

        public IReadOnlyCollection<Game> Games => _games.AsReadOnly();

        public IReadOnlyCollection<PairingParticipant> Participants => _participants.AsReadOnly();

        private Pairing() { }

        public Pairing(
            Guid roundId,
            Guid jassTableId,
            int gamesPerRound,
            PairingStatus status = DefaultPairingStatus)
        {
            Guard.AgainstEmptyGuid(roundId, nameof(roundId));
            Guard.AgainstEmptyGuid(jassTableId, nameof(jassTableId));
            ArgumentOutOfRangeException.ThrowIfLessThan(gamesPerRound, 1);

            RoundId = roundId;
            JassTableId = jassTableId;
            GamesPerRound = gamesPerRound;
            Status = status;
        }

        public void AddGame(Game game)
        {
            ArgumentNullException.ThrowIfNull(game);

            if (game.PairingId != Id)
            {
                throw new InvalidOperationException("Game belongs to a different pairing.");
            }

            if (game.GameNumber > GamesPerRound)
            {
                throw new InvalidOperationException($"Game number {game.GameNumber} exceeds the configured number of games per round ({GamesPerRound}).");
            }

            if (_games.Any(g => g.GameNumber == game.GameNumber))
            {
                throw new InvalidOperationException($"Game number {game.GameNumber} already exists in the pairing.");
            }

            _games.Add(game);
            MarkAsUpdated();
        }

        public void AddParticipant(PairingParticipant participant)
        {
            ArgumentNullException.ThrowIfNull(participant);

            if (participant.PairingId != Id)
            {
                throw new InvalidOperationException("Participant belongs to a different pairing.");
            }

            if (_participants.Count >= RequiredParticipants)
            {
                throw new InvalidOperationException($"Pairing cannot have more than {RequiredParticipants} participants.");
            }

            if (_participants.Any(p => p.TournamentParticipantId == participant.TournamentParticipantId))
            {
                throw new InvalidOperationException("Tournament participant is already assigned to the pairing.");
            }

            if (_participants.Count(p => p.Team == participant.Team) >= RequiredParticipantsPerTeam)
            {
                throw new InvalidOperationException($"Team {participant.Team} already has {RequiredParticipantsPerTeam} participants.");
            }

            _participants.Add(participant);
            MarkAsUpdated();
        }

        public void Complete()
        {
            if (_participants.Count != RequiredParticipants)
            {
                throw new InvalidOperationException($"Pairing must have exactly {RequiredParticipants} participants.");
            }

            if (_participants.Count(p => p.Team == Team.TeamA) != RequiredParticipantsPerTeam ||
                _participants.Count(p => p.Team == Team.TeamB) != RequiredParticipantsPerTeam)
            {
                throw new InvalidOperationException($"Pairing must have exactly {RequiredParticipantsPerTeam} participants per team.");
            }

            if (_games.Count != GamesPerRound)
            {
                throw new InvalidOperationException($"Pairing must have exactly {GamesPerRound} games.");
            }

            if (_games.Any(g => g.Status != GameStatus.Completed))
            {
                throw new InvalidOperationException("Pairing can only be completed when all games are completed.");
            }

            Status = PairingStatus.Completed;
            MarkAsUpdated();
        }

        public void SetBackToPending()
        {
            Status = PairingStatus.Pending;
            MarkAsUpdated();
        }

        public void UpdateMatchBonusEnabledForGames(bool matchBonusEnabled)
        {
            foreach (var game in _games)
            {
                game.UpdateMatchBonusEnabled(matchBonusEnabled);
            }

            MarkAsUpdated();
        }
    }
}
