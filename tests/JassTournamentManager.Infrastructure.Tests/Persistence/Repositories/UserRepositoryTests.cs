using FluentAssertions;
using JassTournamentManager.Domain.Entities;
using JassTournamentManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JassTournamentManager.Infrastructure.Tests.Persistence.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public class UserRepositoryTests
    {
        private readonly PostgreSqlFixture _fixture;

        public UserRepositoryTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        [DockerAvailableFact]
        public async Task GetByIdAsync_WithExistingUser_ReturnsUser()
        {
            await _fixture.ResetDatabaseAsync();

            var user = PersistenceTestData.CreateUser();

            await PersistUser(user);

            await using var assertionContext = _fixture.CreateDbContext();
            var userRepository = new UserRepository(assertionContext);
            var loadedUser = await userRepository.GetByIdAsync(user.Id, CancellationToken.None);

            VerifyUser(loadedUser, user);
        }

        [DockerAvailableFact]
        public async Task GetByIdAsync_WithoutExistingUser_ReturnsNull()
        {
            await _fixture.ResetDatabaseAsync();

            var missingUserId = Guid.NewGuid();

            await using var assertionContext = _fixture.CreateDbContext();
            var userRepository = new UserRepository(assertionContext);
            var loadedUser = await userRepository.GetByIdAsync(missingUserId, CancellationToken.None);

            loadedUser.Should().BeNull();
        }

        [DockerAvailableFact]
        public async Task GetByEmailAsync_WithExistingUser_ReturnsUser()
        {
            await _fixture.ResetDatabaseAsync();

            var user = PersistenceTestData.CreateSelfRegisteredUser();

            await PersistUser(user);

            await using var assertionContext = _fixture.CreateDbContext();
            var userRepository = new UserRepository(assertionContext);
            var loadedUser = await userRepository.GetByEmailAsync(user.Email!, CancellationToken.None);

            VerifyUser(loadedUser, user);
        }

        [DockerAvailableFact]
        public async Task GetByEmailAsync_WithoutExistingUser_ReturnsNull()
        {
            await _fixture.ResetDatabaseAsync();

            var missingUserEmail = "max.muster@email.com";

            await using var assertionContext = _fixture.CreateDbContext();
            var userRepository = new UserRepository(assertionContext);
            var loadedUser = await userRepository.GetByEmailAsync(missingUserEmail, CancellationToken.None);

            loadedUser.Should().BeNull();
        }

        [DockerAvailableFact]
        public async Task ExistsAsync_WithExistentUser_ReturnsTrue()
        {
            await _fixture.ResetDatabaseAsync();
            var user = PersistenceTestData.CreateUser();

            await using (var dbContext = _fixture.CreateDbContext())
            {
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
            }

            await using var assertionContext = _fixture.CreateDbContext();
            var repository = new UserRepository(assertionContext);

            var exists = await repository.ExistsAsync(user.Id, CancellationToken.None);

            exists.Should().BeTrue();
        }

        [DockerAvailableFact]
        public async Task ExistsAsync_WithNonExistentUser_ReturnsFalse()
        {
            await _fixture.ResetDatabaseAsync();
            var missingUserId = Guid.NewGuid();

            await using var assertionContext = _fixture.CreateDbContext();
            var repository = new UserRepository(assertionContext);

            var exists = await repository.ExistsAsync(missingUserId, CancellationToken.None);

            exists.Should().BeFalse();
        }

        [DockerAvailableFact]
        public async Task ExistsByEmailAsync_WithExistentUser_ReturnsTrue()
        {
            await _fixture.ResetDatabaseAsync();
            var user = PersistenceTestData.CreateSelfRegisteredUser();

            await using (var dbContext = _fixture.CreateDbContext())
            {
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
            }

            await using var assertionContext = _fixture.CreateDbContext();
            var repository = new UserRepository(assertionContext);

            var exists = await repository.ExistsByEmailAsync(user.Email!, CancellationToken.None);

            exists.Should().BeTrue();
        }

        [DockerAvailableFact]
        public async Task ExistsByEmailAsync_WithNonExistentUser_ReturnsFalse()
        {
            await _fixture.ResetDatabaseAsync();
            var missingUserEmail = "max.muster@email.com";

            await using var assertionContext = _fixture.CreateDbContext();
            var repository = new UserRepository(assertionContext);

            var exists = await repository.ExistsByEmailAsync(missingUserEmail, CancellationToken.None);

            exists.Should().BeFalse();
        }

        [DockerAvailableFact]
        public async Task AddAsync_PersistsUser()
        {
            await _fixture.ResetDatabaseAsync();
            var user = PersistenceTestData.CreateSelfRegisteredUser();

            await PersistUser(user);

            await using var assertionContext = _fixture.CreateDbContext();
            var loadedUser = await assertionContext.Users.SingleAsync(CancellationToken.None);
            VerifyUser(loadedUser, user);
        }

        [DockerAvailableFact]
        public async Task GetClaimableUsersAsync_WithClaimableAndNonClaimableUsers_ReturnsOnlyClaimableUsers()
        {
            await _fixture.ResetDatabaseAsync();
            var claimableUser1 = PersistenceTestData.CreateUser();
            var claimableUser2 = PersistenceTestData.CreateUser();
            var nonClaimableUser1 = PersistenceTestData.CreateSelfRegisteredUser();
            var nonClaimableUser2 = PersistenceTestData.CreateUser(isActive: false);
            var nonClaimableUser3 = PersistenceTestData.CreateUser();
            nonClaimableUser3.MergeIntoDifferentUser(claimableUser1.Id, nonClaimableUser2.Id);

            await using (var dbContext = _fixture.CreateDbContext())
            {
                dbContext.Users.AddRange(claimableUser1, claimableUser2, nonClaimableUser1, nonClaimableUser2, nonClaimableUser3);
                await dbContext.SaveChangesAsync();
            }

            await using var assertionContext = _fixture.CreateDbContext();
            var repository = new UserRepository(assertionContext);
            var claimableUsers = await repository.GetClaimableUsersAsync(CancellationToken.None);

            claimableUsers.Should().HaveCount(2);
            VerifyUser(claimableUsers.Single(u => u.Id == claimableUser1.Id), claimableUser1);
            VerifyUser(claimableUsers.Single(u => u.Id == claimableUser2.Id), claimableUser2);
        }

        private async Task PersistUser(User user)
        {
            await using var dbContext = _fixture.CreateDbContext();
            var userRepository = new UserRepository(dbContext);

            await userRepository.AddAsync(user, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        private static void VerifyUser(User? user, User userToVerifyAgainst)
        {
            user.Should().NotBeNull();

            user.Id.Should().Be(userToVerifyAgainst.Id);
            user.Email.Should().Be(userToVerifyAgainst.Email);
            user.PasswordHash.Should().Be(userToVerifyAgainst.PasswordHash);
            user.FirstName.Should().Be(userToVerifyAgainst.FirstName);
            user.LastName.Should().Be(userToVerifyAgainst.LastName);
        }
    }
}
