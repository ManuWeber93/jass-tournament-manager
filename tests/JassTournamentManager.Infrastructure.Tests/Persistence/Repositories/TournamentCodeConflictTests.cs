using FluentAssertions;
using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Infrastructure.Persistence;
using JassTournamentManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace JassTournamentManager.Infrastructure.Tests.Persistence.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public class TournamentCodeConflictTests
    {
        private readonly PostgreSqlFixture _fixture;

        public TournamentCodeConflictTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        [DockerAvailableFact]
        public async Task SaveChangesAsync_WithCodeCollision_CanRetrySameTrackedTournament()
        {
            await _fixture.ResetDatabaseAsync();
            var organizer = PersistenceTestData.CreateUser();
            var existing = PersistenceTestData.CreateTournament(organizer.Id, "COLLISION");
            await using (var seedContext = _fixture.CreateDbContext())
            {
                seedContext.Users.Add(organizer);
                seedContext.Tournaments.Add(existing);
                await seedContext.SaveChangesAsync();
            }

            var tournament = PersistenceTestData.CreateTournament(organizer.Id, existing.Details.TournamentCode);
            var round = PersistenceTestData.CreateRound(tournament.Id);
            tournament.AddRound(round);
            await using var dbContext = _fixture.CreateDbContext();
            await new TournamentRepository(dbContext).AddAsync(tournament, CancellationToken.None);
            var unitOfWork = new UnitOfWork(dbContext);
            var act = () => unitOfWork.SaveChangesAsync(CancellationToken.None);

            var conflict = await act.Should().ThrowAsync<TournamentCodeConflictException>();
            conflict.Which.InnerException.Should().BeOfType<DbUpdateException>()
                .Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.ConstraintName.Should().Be("ux_tournaments_tournament_code");
            dbContext.Entry(tournament).State.Should().Be(EntityState.Added);
            tournament.UpdateDetails(tournament.Details with { TournamentCode = "RETRY-CODE" });

            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            await using var assertionContext = _fixture.CreateDbContext();
            var loaded = await new TournamentRepository(assertionContext)
                .GetByIdAsync(tournament.Id, CancellationToken.None);
            loaded.Should().NotBeNull();
            loaded!.Details.Should().BeEquivalentTo(tournament.Details);
            loaded.Rounds.Should().ContainSingle().Which.Id.Should().Be(round.Id);
            (await assertionContext.Tournaments.CountAsync()).Should().Be(2);
        }

        [DockerAvailableFact]
        public async Task SaveChangesAsync_WithUnrelatedUniqueViolation_PreservesDatabaseException()
        {
            await _fixture.ResetDatabaseAsync();
            var email = "duplicate@example.com";
            await using var dbContext = _fixture.CreateDbContext();
            dbContext.Users.AddRange(
                PersistenceTestData.CreateSelfRegisteredUser(email),
                PersistenceTestData.CreateSelfRegisteredUser(email));
            var unitOfWork = new UnitOfWork(dbContext);

            var act = () => unitOfWork.SaveChangesAsync(CancellationToken.None);

            var result = await act.Should().ThrowAsync<DbUpdateException>();
            result.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        }
    }
}