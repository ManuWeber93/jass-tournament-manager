using System.ComponentModel;
using JassTournamentManager.Contracts.Auth;
using JassTournamentManager.Contracts.Users;

namespace JassTournamentManager.App.Infrastructure.Auth
{
    public interface IAuthSession : INotifyPropertyChanged
    {
        UserResponse? CurrentUser { get; }
        bool LogoutWarning { get; }
        Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<LoginResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task LogoutAsync(CancellationToken cancellationToken = default);
    }
}