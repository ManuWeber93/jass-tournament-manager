using FluentAssertions;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Tests.Support;
using JassTournamentManager.Contracts.Auth;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure
{
    public sealed class LoginResultTests
    {
        [Fact]
        public void Constructor_PreservesProvidedValues()
        {
            AuthResponse session = AuthTestData.CreateResponse(AuthTestData.CreateTokens());
            const AuthFailure failure = AuthFailure.Server;

            var result = new LoginResult(session, failure);

            result.Session.Should().BeSameAs(session);
            result.Failure.Should().Be(failure);
        }

        [Fact]
        public void Success_ReturnsProvidedSessionWithoutFailure()
        {
            AuthResponse session = AuthTestData.CreateResponse(AuthTestData.CreateTokens());

            LoginResult result = LoginResult.Success(session);

            result.Session.Should().BeSameAs(session);
            result.Failure.Should().Be(AuthFailure.None);
        }
    }
}