using JassTournamentManager.Domain.Entities;

namespace JassTournamentManager.Domain.Tests.TestHelpers
{
    public static class RefreshTokenTestData
    {
        public static Guid CreateRefreshTokenId() => Guid.NewGuid();

        public static string CreateTokenHash() => "alsfjsaldfjaslfjsdfk";

        public static DateTimeOffset CreateExpiresAtUtc() => DateTimeOffset.UtcNow.AddDays(7);

        public static RefreshToken CreateRefreshToken() => new(
            UserTestData.CreateUserId(),
            CreateTokenHash(),
            CreateExpiresAtUtc());
    }
}
