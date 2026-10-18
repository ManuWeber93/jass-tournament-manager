using JassTournamentManager.Contracts.Auth;

namespace JassTournamentManager.App.Infrastructure.Auth
{
    public sealed record LoginResult(AuthResponse? Session, AuthFailure Failure)
    {
        public static LoginResult Success(AuthResponse session) => new(session, AuthFailure.None);
    }
}