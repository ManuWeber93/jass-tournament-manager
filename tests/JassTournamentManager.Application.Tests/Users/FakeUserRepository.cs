using JassTournamentManager.Application.Users;
using JassTournamentManager.Domain.Entities;

namespace JassTournamentManager.Application.Tests.Users
{
    internal sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public IReadOnlyCollection<User> Users => _users.AsReadOnly();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.Email == email));
        }

        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.Any(user => user.Id == userId));
        }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.Any(user => user.Email == email));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<User>> GetClaimableUsersAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<User> claimableUsers = _users
                .Where(user => user.CanBeClaimed())
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .ToList()
                .AsReadOnly();

            return Task.FromResult(claimableUsers);
        }
    }
}
