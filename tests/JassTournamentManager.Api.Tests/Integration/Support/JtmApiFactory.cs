using JassTournamentManager.Application.Auth;
using JassTournamentManager.Application.TournamentTemplates;
using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JassTournamentManager.Api.Tests.Integration.Support
{
    internal sealed class JtmApiFactory : WebApplicationFactory<Program>
    {
        public FakeServices FakeServices { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                var testConfiguration = new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "jass-tournament-manager-tests",
                    ["Jwt:Audience"] = "jass-tournament-manager-api-tests",
                    ["Jwt:Secret"] = "test-jwt-secret-with-at-least-32-characters",
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["Jwt:RefreshTokenDays"] = "30"
                };

                configurationBuilder.AddInMemoryCollection(testConfiguration);
            });

            builder.ConfigureTestServices(services =>
            {
                ReplaceApplicationServices(services);
                AddTestAuthentication(services);
            });
        }

        private void ReplaceApplicationServices(IServiceCollection services)
        {
            ReplaceWithSingleton<IAuthService>(services, FakeServices.AuthService);
            ReplaceWithSingleton<ITournamentTemplateService>(services, FakeServices.TournamentTemplateService);
            ReplaceWithSingleton<ITournamentService>(services, FakeServices.TournamentService);
            ReplaceWithSingleton<IUserService>(services, FakeServices.UserService);
        }

        private static void ReplaceWithSingleton<TService>(IServiceCollection services, TService implementation)
            where TService : class
        {
            services.RemoveAll<TService>();
            services.AddSingleton(implementation);
        }

        private static void AddTestAuthentication(IServiceCollection services)
        {
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigureAll<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });
        }
    }
}


