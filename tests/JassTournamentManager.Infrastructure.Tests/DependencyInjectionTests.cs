using FluentAssertions;
using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Infrastructure.Persistence;
using JassTournamentManager.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace JassTournamentManager.Infrastructure.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void AddInfrastructure_RegistersTournamentRepositoryAndUnitOfWorkAsScoped()
        {
            var services = new ServiceCollection();
            services.AddInfrastructure("Host=localhost;Database=unused");
            using var provider = services.BuildServiceProvider();
            using var firstScope = provider.CreateScope();
            using var secondScope = provider.CreateScope();

            var repository = firstScope.ServiceProvider.GetRequiredService<ITournamentRepository>();
            var unitOfWork = firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            repository.Should().BeOfType<TournamentRepository>();
            repository.Should().BeSameAs(firstScope.ServiceProvider.GetRequiredService<ITournamentRepository>());
            repository.Should().NotBeSameAs(secondScope.ServiceProvider.GetRequiredService<ITournamentRepository>());
            unitOfWork.Should().BeOfType<UnitOfWork>();
            unitOfWork.Should().BeSameAs(firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>());
            unitOfWork.Should().NotBeSameAs(secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        }
    }
}