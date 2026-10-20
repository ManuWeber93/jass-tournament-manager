using JassTournamentManager.Application.Auth;
using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Application.TournamentTemplates;
using JassTournamentManager.Application.Users;
using JassTournamentManager.Infrastructure.Auth;
using JassTournamentManager.Infrastructure.Persistence;
using JassTournamentManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JassTournamentManager.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
        {
            services.AddDbContext<JtmDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "jtm")));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITournamentRepository, TournamentRepository>();
            services.AddScoped<ITournamentTemplateRepository, TournamentTemplateRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddScoped<IUserPasswordHasher, UserPasswordHasher>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();

            return services;
        }
    }
}
