using FluentAssertions;
using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace JassTournamentManager.Infrastructure.Tests.Persistence
{
    public class UnitOfWorkTests
    {
        [Fact]
        public async Task SaveChangesAsync_WithTournamentCodeUniqueViolation_TranslatesException()
        {
            var databaseException = CreateDatabaseException(
                PostgresErrorCodes.UniqueViolation, "ux_tournaments_tournament_code");
            await using var dbContext = CreateFailingContext(databaseException);
            var unitOfWork = new UnitOfWork(dbContext);

            var act = () => unitOfWork.SaveChangesAsync(CancellationToken.None);

            var result = await act.Should().ThrowAsync<TournamentCodeConflictException>();
            result.Which.InnerException.Should().BeSameAs(databaseException);
        }

        [Theory]
        [InlineData(PostgresErrorCodes.UniqueViolation, "ux_users_email")]
        [InlineData(PostgresErrorCodes.UniqueViolation, null)]
        [InlineData(PostgresErrorCodes.ForeignKeyViolation, "ux_tournaments_tournament_code")]
        public async Task SaveChangesAsync_WithOtherPostgresError_PreservesException(
            string sqlState, string? constraintName)
        {
            var databaseException = CreateDatabaseException(sqlState, constraintName);
            await AssertExceptionPreservedAsync(databaseException);
        }

        [Fact]
        public async Task SaveChangesAsync_WithOtherUpdateError_PreservesException()
        {
            await AssertExceptionPreservedAsync(new DbUpdateException("Other update failure"));
        }

        [Fact]
        public async Task SaveChangesAsync_WithCancellation_PreservesException()
        {
            await AssertExceptionPreservedAsync(new OperationCanceledException());
        }

        private static DbUpdateException CreateDatabaseException(string sqlState, string? constraintName)
        {
            return new DbUpdateException("Save failed", new PostgresException(
                "Constraint violation", "ERROR", "ERROR", sqlState, constraintName: constraintName));
        }

        private static async Task AssertExceptionPreservedAsync(Exception exception)
        {
            await using var dbContext = CreateFailingContext(exception);
            var unitOfWork = new UnitOfWork(dbContext);

            var act = () => unitOfWork.SaveChangesAsync(CancellationToken.None);

            var result = await act.Should().ThrowAsync<Exception>();
            result.Which.Should().BeSameAs(exception);
        }

        private static JtmDbContext CreateFailingContext(Exception exception)
        {
            var options = new DbContextOptionsBuilder<JtmDbContext>()
                .UseNpgsql("Host=localhost;Database=unused")
                .AddInterceptors(new FailingSaveChangesInterceptor(exception))
                .Options;
            return new JtmDbContext(options);
        }

        private sealed class FailingSaveChangesInterceptor(Exception exception) : SaveChangesInterceptor
        {
            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                throw exception;
            }
        }
    }
}