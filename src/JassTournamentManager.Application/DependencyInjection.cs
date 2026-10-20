using JassTournamentManager.Application.Auth;
using JassTournamentManager.Application.TournamentTemplates;
using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace JassTournamentManager.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ITournamentTemplateService, TournamentTemplateService>();
            services.AddScoped<ITournamentService, TournamentService>();
            services.AddSingleton<ITournamentCodeGenerator, RandomTournamentCodeGenerator>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}
