using FluentAssertions;
using JassTournamentManager.Domain.Enums;
using JassTournamentManager.Domain.Services;
using JassTournamentManager.Domain.Tests.TestHelpers;
using JassTournamentManager.Domain.ValueObjects;

namespace JassTournamentManager.Domain.Tests.Services
{
    public class TournamentFactoryTests
    {
        [Fact]
        public void Create_WithValidValues_PreservesProvidedValues()
        {
            var organizerId = UserTestData.CreateUserId();
            var details = new TournamentDetails(
                "Summer Tournament", "Zurich", new DateOnly(2026, 7, 18), "SUMMER26");
            var configValues = new TournamentConfigValues(
                numberOfRounds: 4,
                gamesPerRound: 6,
                matchBonusEnabled: false,
                isFixedTeams: true,
                scoreVisibility: ScoreVisibility.OrganizerOnly);

            var tournament = TournamentFactory.Create(organizerId, details, configValues);

            tournament.OrganizerId.Should().Be(organizerId);
            tournament.Details.Should().Be(details);
            tournament.ConfigValues.Should().Be(configValues);
        }

        [Fact]
        public void Create_WithDefaultConfig_PreservesDefaultsAndCreatesAllRounds()
        {
            var organizerId = UserTestData.CreateUserId();
            var details = TournamentTestData.CreateTournamentDetails();
            var configValues = new TournamentConfigValues();

            var tournament = TournamentFactory.Create(organizerId, details, configValues);

            tournament.ConfigValues.Should().Be(configValues);
            tournament.Rounds.Should().HaveCount(configValues.NumberOfRounds);
        }

        [Fact]
        public void Create_DefaultStatus_IsActive()
        {
            var tournament = TournamentFactory.Create(
                UserTestData.CreateUserId(),
                TournamentTestData.CreateTournamentDetails(),
                TournamentTestData.CreateTournamentConfigValues());

            tournament.Status.Should().Be(TournamentStatus.Active);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(8)]
        public void Create_WithRequestedRoundCount_CreatesSequentialPendingRoundsForTournament(int numberOfRounds)
        {
            var organizerId = UserTestData.CreateUserId();
            var details = TournamentTestData.CreateTournamentDetails();
            var configValues = new TournamentConfigValues(numberOfRounds: numberOfRounds);

            var tournament = TournamentFactory.Create(organizerId, details, configValues);

            tournament.Id.Should().NotBeEmpty();
            tournament.Rounds.Should().HaveCount(numberOfRounds);
            tournament.ConfigValues.NumberOfRounds.Should().Be(numberOfRounds);
            tournament.Rounds.Select(round => round.RoundNumber)
                .Should().Equal(Enumerable.Range(1, numberOfRounds));
            tournament.Rounds.Select(round => round.Id).Should().OnlyHaveUniqueItems();
            tournament.Rounds.Should().AllSatisfy(round =>
            {
                round.Id.Should().NotBeEmpty();
                round.TournamentId.Should().Be(tournament.Id);
                round.Status.Should().Be(RoundStatus.Pending);
                round.Pairings.Should().BeEmpty();
            });
        }

        [Fact]
        public void Create_AddsOrganizerAsPlayingParticipant()
        {
            var organizerId = UserTestData.CreateUserId();
            var details = TournamentTestData.CreateTournamentDetails();
            var configValues = TournamentTestData.CreateTournamentConfigValues();

            var tournament = TournamentFactory.Create(organizerId, details, configValues);

            var participant = tournament.Participants.Should().ContainSingle().Subject;
            participant.UserId.Should().Be(organizerId);
            participant.TournamentId.Should().Be(tournament.Id);
            participant.Role.Should().Be(ParticipantRole.Organizer);
            participant.RegistrationMethod.Should().Be(RegistrationMethod.ByOrganizer);
            participant.IsPlaying.Should().BeTrue();
        }

        [Fact]
        public void Create_WithNullDetails_ThrowsArgumentNullException()
        {
            var organizerId = UserTestData.CreateUserId();
            var configValues = TournamentTestData.CreateTournamentConfigValues();

            Action act = () => TournamentFactory.Create(organizerId, null!, configValues);

            act.Should().Throw<ArgumentNullException>().WithParameterName("details");
        }

        [Fact]
        public void Create_WithNullConfigValues_ThrowsArgumentNullException()
        {
            var organizerId = UserTestData.CreateUserId();
            var details = TournamentTestData.CreateTournamentDetails();

            Action act = () => TournamentFactory.Create(organizerId, details, null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("configValues");
        }

        [Fact]
        public void Create_WithEmptyOrganizerId_ThrowsArgumentException()
        {
            var details = TournamentTestData.CreateTournamentDetails();
            var configValues = TournamentTestData.CreateTournamentConfigValues();

            Action act = () => TournamentFactory.Create(Guid.Empty, details, configValues);

            act.Should().Throw<ArgumentException>().WithParameterName("organizerId");
        }

        [Fact]
        public void Create_CalledTwiceWithSameInputs_CreatesIndependentTournamentsAndRounds()
        {
            var organizerId = UserTestData.CreateUserId();
            var details = TournamentTestData.CreateTournamentDetails();
            var numberOfRounds = 3;
            var configValues = new TournamentConfigValues(numberOfRounds: numberOfRounds);

            var firstTournament = TournamentFactory.Create(organizerId, details, configValues);
            var secondTournament = TournamentFactory.Create(organizerId, details, configValues);

            firstTournament.Id.Should().NotBe(secondTournament.Id);
            firstTournament.Rounds.Select(round => round.Id)
                .Should().NotIntersectWith(secondTournament.Rounds.Select(round => round.Id));

            firstTournament.Rounds.First().Activate();
            firstTournament.RemoveRound(firstTournament.Rounds.Last().Id);

            secondTournament.Rounds.Should().HaveCount(configValues.NumberOfRounds);
            secondTournament.Rounds.Should().AllSatisfy(round => round.Status.Should().Be(RoundStatus.Pending));
            secondTournament.ConfigValues.Should().Be(configValues);
            configValues.NumberOfRounds.Should().Be(numberOfRounds);
        }
    }
}