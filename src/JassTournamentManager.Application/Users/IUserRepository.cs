using JassTournamentManager.Domain.Entities;

namespace JassTournamentManager.Application.Users
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

        Task AddAsync(User user, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<User>> GetClaimableUsersAsync(CancellationToken cancellationToken);
    }
}
