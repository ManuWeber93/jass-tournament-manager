using FluentAssertions;
using JassTournamentManager.Api.Controllers;

namespace JassTournamentManager.Api.Tests.Tournaments
{
    public class TournamentsControllerTests
    {
        [Fact]
        public void Constructor_WithNullService_ThrowsArgumentNullException()
        {
            Action action = () => new TournamentsController(null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("tournamentService");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Endpoint_WhenServiceObservesCancellation_PropagatesCancellation(bool create)
        {
            var service = new FakeTournamentService();
            var controller = new TournamentsController(service);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Func<Task> action = async () =>
            {
                if (create)
                {
                    await controller.CreateAsync(TournamentsControllerTestData.CreateRequest(), cancellation.Token);
                }
                else
                {
                    await controller.GetByIdAsync(Guid.NewGuid(), cancellation.Token);
                }
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
            service.ReceivedCancellationToken.Should().Be(cancellation.Token);
        }
    }
}