using JassTournamentManager.Domain.Entities;

namespace JassTournamentManager.Domain.Tests.TestHelpers
{
    internal static class JassTableTestData
    {
        public static Guid CreateJassTableId() => Guid.NewGuid();

        public static string CreateJassTableName() => "Nyffis";

        public static int CreateDisplayOrder() => 1;

        public static JassTable CreateJassTable() => new(
            UserTestData.CreateUserId(),
            CreateJassTableName(),
            CreateDisplayOrder());
    }
}
