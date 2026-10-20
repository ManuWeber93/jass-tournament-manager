using FluentAssertions;
using JassTournamentManager.Domain.Entities;
using JassTournamentManager.Domain.Enums;
using JassTournamentManager.Domain.Services;
using JassTournamentManager.Infrastructure.Persistence;
using JassTournamentManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JassTournamentManager.Infrastructure.Tests.Persistence.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public class TournamentRepositoryTests
    {
        private readonly PostgreSqlFixture _fixture;

        public TournamentRepositoryTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        [DockerAvailableFact]
        public async Task AddAsync_WithFactoryTournament_PersistsOrganizerParticipationAndRounds()
        {
            await _fixture.ResetDatabaseAsync();
            var organizer = PersistenceTestData.CreateUser();
            var arranged = PersistenceTestData.CreateTournament(organizer.Id, "ORGANIZER");
            var tournament = TournamentFactory.Create(organizer.Id, arranged.Details, arranged.ConfigValues);
            var participant = tournament.Participants.Single();

            await using (var dbContext = _fixture.CreateDbContext())
            {
                dbContext.Users.Add(organizer);
                await new TournamentRepository(dbContext).AddAsync(tournament, CancellationToken.None);
                await new UnitOfWork(dbContext).SaveChangesAsync(CancellationToken.None);
            }

            await using var assertionContext = _fixture.CreateDbContext();
            var loaded = await new TournamentRepository(assertionContext)
                .GetByIdAsync(tournament.Id, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.Rounds.Should().HaveCount(tournament.ConfigValues.NumberOfRounds);
            var loadedParticipant = loaded.Participants.Should().ContainSingle().Subject;
            loadedParticipant.Id.Should().Be(participant.Id);
            loadedParticipant.TournamentId.Should().Be(tournament.Id);
            loadedParticipant.UserId.Should().Be(organizer.Id);
            loadedParticipant.Role.Should().Be(participant.Role);
            loadedParticipant.RegistrationMethod.Should().Be(participant.RegistrationMethod);
            loadedParticipant.IsPlaying.Should().Be(participant.IsPlaying);
        }

        [DockerAvailableFact]
        public async Task AddAsync_WithRoundGraph_DoesNotSaveUntilUnitOfWorkSaves()
        {
            await _fixture.ResetDatabaseAsync();
            var organizer = PersistenceTestData.CreateUser();
            var tournament = PersistenceTestData.CreateTournament(organizer.Id, "ROUND-GRAPH");
            var rounds = Enumerable.Range(1, 3)
                .Select(number => PersistenceTestData.CreateRound(tournament.Id, number))
                .ToArray();
            foreach (var round in rounds)
            {
                tournament.AddRound(round);
            }

            await using var dbContext = _fixture.CreateDbContext();
            dbContext.Users.Add(organizer);
            var repository = new TournamentRepository(dbContext);

            await repository.AddAsync(tournament, CancellationToken.None);

            dbContext.Entry(tournament).State.Should().Be(EntityState.Added);
            await using (var beforeSaveContext = _fixture.CreateDbContext())
            {
                (await beforeSaveContext.Tournaments.AnyAsync()).Should().BeFalse();
                (await beforeSaveContext.Rounds.AnyAsync()).Should().BeFalse();
            }

            await new UnitOfWork(dbContext).SaveChangesAsync(CancellationToken.None);

            await using var assertionContext = _fixture.CreateDbContext();
            var loaded = await new TournamentRepository(assertionContext)
                .GetByIdAsync(tournament.Id, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.Id.Should().Be(tournament.Id);
            loaded.OrganizerId.Should().Be(organizer.Id);
            loaded.Details.Should().BeEquivalentTo(tournament.Details);
            loaded.ConfigValues.Should().BeEquivalentTo(tournament.ConfigValues);
            loaded.Status.Should().Be(tournament.Status);
            loaded.Rounds.Should().BeEquivalentTo(rounds, options => options
                .Using<DateTimeOffset>(context => context.Subject.Should()
                    .BeCloseTo(context.Expectation, TimeSpan.FromMicroseconds(1)))
                .WhenTypeIs<DateTimeOffset>());
            assertionContext.Entry(loaded).State.Should().Be(EntityState.Unchanged);
        }

        [DockerAvailableFact]
        public async Task GetByIdAsync_WithParticipants_LoadsAccessData()
        {
            await _fixture.ResetDatabaseAsync();
            var organizer = PersistenceTestData.CreateUser();
            var player = PersistenceTestData.CreateUser();
            var tournament = PersistenceTestData.CreateTournament(organizer.Id, "ACCESS-DATA");
            var participants = new[]
            {
                new TournamentParticipant(tournament.Id, organizer.Id,
                    RegistrationMethod.ByOrganizer, ParticipantRole.Organizer, isPlaying: false),
                new TournamentParticipant(tournament.Id, player.Id,
                    RegistrationMethod.ByOrganizer, ParticipantRole.Player, isPlaying: true)
            };
            foreach (var participant in participants)
            {
                tournament.AddParticipant(participant);
            }

            await using (var dbContext = _fixture.CreateDbContext())
            {
                dbContext.Users.AddRange(organizer, player);
                await new TournamentRepository(dbContext).AddAsync(tournament, CancellationToken.None);
                await new UnitOfWork(dbContext).SaveChangesAsync(CancellationToken.None);
            }

            await using var assertionContext = _fixture.CreateDbContext();
            var loaded = await new TournamentRepository(assertionContext)
                .GetByIdAsync(tournament.Id, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.Participants.Should().BeEquivalentTo(participants, options => options
                .Using<DateTimeOffset>(context => context.Subject.Should()
                    .BeCloseTo(context.Expectation, TimeSpan.FromMicroseconds(1)))
                .WhenTypeIs<DateTimeOffset>());
        }

        [DockerAvailableFact]
        public async Task GetByIdAsync_WithMissingTournament_ReturnsNull()
        {
            await _fixture.ResetDatabaseAsync();
            await using var dbContext = _fixture.CreateDbContext();

            var loaded = await new TournamentRepository(dbContext)
                .GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

            loaded.Should().BeNull();
        }

        [DockerAvailableFact]
        public async Task ExistsByCodeAsync_WithExistingAndMissingCodes_ReturnsMatchingResults()
        {
            await _fixture.ResetDatabaseAsync();
            var organizer = PersistenceTestData.CreateUser();
            var tournament = PersistenceTestData.CreateTournament(organizer.Id, "EXISTING-CODE");
            await using (var dbContext = _fixture.CreateDbContext())
            {
                dbContext.Users.Add(organizer);
                await new TournamentRepository(dbContext).AddAsync(tournament, CancellationToken.None);
                await new UnitOfWork(dbContext).SaveChangesAsync(CancellationToken.None);
            }

            await using var assertionContext = _fixture.CreateDbContext();
            var repository = new TournamentRepository(assertionContext);

            (await repository.ExistsByCodeAsync(tournament.Details.TournamentCode, CancellationToken.None))
                .Should().BeTrue();
            (await repository.ExistsByCodeAsync("MISSING-CODE", CancellationToken.None))
                .Should().BeFalse();
        }
    }
}