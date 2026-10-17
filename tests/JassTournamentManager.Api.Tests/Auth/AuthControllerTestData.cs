using JassTournamentManager.Api.Tests.Users;
using JassTournamentManager.Contracts.Auth;

namespace JassTournamentManager.Api.Tests.Auth
{
    internal static class AuthControllerTestData
    {
        public static Guid CreateClaimedUserId() => Guid.NewGuid();

        public static string CreateAccessToken() => "access-token";

        public static DateTimeOffset CreateAccessTokenExpiresAt() => DateTimeOffset.Now.AddMinutes(15);

        public static string CreateRefreshToken() => "refresh-token";

        public static DateTimeOffset CreateRefreshTokenExpiresAt() => new(2043, 11, 30, 2, 0, 0, TimeSpan.Zero);

        public static RegisterRequest CreateRegisterRequest() => new(
            null,
            UsersControllerTestData.CreateEmail(),
            UsersControllerTestData.CreatePassword(),
            UsersControllerTestData.CreateFirstName(),
            UsersControllerTestData.CreateLastName());

        public static RegisterRequest CreateRegisterRequestWithUserClaiming() => new(
            CreateClaimedUserId(),
            UsersControllerTestData.CreateEmail(),
            UsersControllerTestData.CreatePassword(),
            UsersControllerTestData.CreateFirstName(),
            UsersControllerTestData.CreateLastName());

        public static AuthResponse CreateAuthResponse() => new(
            CreateAccessToken(),
            CreateAccessTokenExpiresAt(),
            CreateRefreshToken(),
            CreateRefreshTokenExpiresAt(),
            UsersControllerTestData.CreateUserResponse()
            );

        public static LoginRequest CreateLoginRequest() => new(
            UsersControllerTestData.CreateEmail(),
            UsersControllerTestData.CreatePassword());

        public static RefreshSessionRequest CreateRefreshSessionRequest() => new(
            CreateRefreshToken());

        public static LogoutRequest CreateLogoutRequest() => new(
            CreateRefreshToken());
    }
}
