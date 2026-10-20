using JassTournamentManager.Api.Tests.Auth;
using JassTournamentManager.Api.Tests.TournamentTemplates;
using JassTournamentManager.Api.Tests.Tournaments;
using JassTournamentManager.Api.Tests.Users;

namespace JassTournamentManager.Api.Tests.Integration.Support
{
    internal sealed class FakeServices
    {
        public FakeAuthService AuthService { get; } = new();

        public FakeTournamentTemplateService TournamentTemplateService { get; } = new();

        public FakeTournamentService TournamentService { get; } = new();

        public FakeUserService UserService { get; } = new();
    }
}


