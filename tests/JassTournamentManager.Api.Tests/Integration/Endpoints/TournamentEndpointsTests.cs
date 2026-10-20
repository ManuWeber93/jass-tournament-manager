using FluentAssertions;
using JassTournamentManager.Api.Tests.Integration.Support;
using JassTournamentManager.Api.Tests.Tournaments;
using JassTournamentManager.Application.Common;
using JassTournamentManager.Contracts.Tournaments;
using System.Net;
using System.Net.Http.Json;

namespace JassTournamentManager.Api.Tests.Integration.Endpoints
{
    public class TournamentEndpointsTests
    {
        [Theory]
        [InlineData("POST")]
        [InlineData("GET")]
        public async Task Endpoint_WithoutAuthentication_ReturnsUnauthorizedWithoutCallingService(string method)
        {
            await using var factory = new JtmApiFactory();
            using HttpClient client = factory.CreateClient();
            using HttpRequestMessage request = CreateRequest(method);

            using HttpResponseMessage response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            factory.FakeServices.TournamentService.CreateAsyncCallCount.Should().Be(0);
            factory.FakeServices.TournamentService.GetByIdAsyncCallCount.Should().Be(0);
        }

        [Fact]
        public async Task Create_WithValidRequest_ReturnsCreatedWithResponseAndResolvableLocation()
        {
            await using var factory = new JtmApiFactory();
            using HttpClient client = factory.CreateClient();
            CreateTournamentRequest createRequest = TournamentsControllerTestData.CreateRequest();
            TournamentResponse expected = TournamentsControllerTestData.CreateResponse(createRequest);
            FakeTournamentService service = factory.FakeServices.TournamentService;
            service.CreateAsyncResult = Result<TournamentResponse>.Success(expected);
            service.GetByIdAsyncResult = Result<TournamentResponse>.Success(expected);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tournaments")
            {
                Content = JsonContent.Create(createRequest)
            };
            request.AddAuthenticatedUser();

            using HttpResponseMessage response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.AbsolutePath.Should().Be($"/api/tournaments/{expected.Id}");
            (await response.Content.ReadFromJsonAsync<TournamentResponse>()).Should().BeEquivalentTo(expected);
            service.CreateAsyncCallCount.Should().Be(1);
            service.ReceivedCreateAsyncRequest.Should().BeEquivalentTo(createRequest);
            service.ReceivedCancellationToken.CanBeCanceled.Should().BeTrue();

            using var getRequest = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
            getRequest.AddAuthenticatedUser();
            using HttpResponseMessage getResponse = await client.SendAsync(getRequest);

            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            (await getResponse.Content.ReadFromJsonAsync<TournamentResponse>()).Should().BeEquivalentTo(expected);
            service.ReceivedGetByIdAsyncRequest.Should().Be(expected.Id);
        }

        [Fact]
        public async Task GetById_WithExistingTournament_ReturnsResponse()
        {
            await using var factory = new JtmApiFactory();
            using HttpClient client = factory.CreateClient();
            TournamentResponse expected = TournamentsControllerTestData.CreateResponse(TournamentsControllerTestData.CreateRequest());
            FakeTournamentService service = factory.FakeServices.TournamentService;
            service.GetByIdAsyncResult = Result<TournamentResponse>.Success(expected);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/tournaments/{expected.Id}");
            request.AddAuthenticatedUser();

            using HttpResponseMessage response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadFromJsonAsync<TournamentResponse>()).Should().BeEquivalentTo(expected);
            service.GetByIdAsyncCallCount.Should().Be(1);
            service.ReceivedGetByIdAsyncRequest.Should().Be(expected.Id);
            service.ReceivedCancellationToken.CanBeCanceled.Should().BeTrue();
        }

        [Theory]
        [InlineData("POST", ErrorType.Invalid, HttpStatusCode.BadRequest)]
        [InlineData("POST", ErrorType.Forbidden, HttpStatusCode.Forbidden)]
        [InlineData("POST", ErrorType.NotFound, HttpStatusCode.NotFound)]
        [InlineData("POST", ErrorType.Conflict, HttpStatusCode.Conflict)]
        [InlineData("GET", ErrorType.Invalid, HttpStatusCode.BadRequest)]
        [InlineData("GET", ErrorType.Forbidden, HttpStatusCode.Forbidden)]
        [InlineData("GET", ErrorType.NotFound, HttpStatusCode.NotFound)]
        [InlineData("GET", ErrorType.Conflict, HttpStatusCode.Conflict)]
        public async Task Endpoint_WithServiceError_ReturnsMappedStatusAndError(string method, ErrorType errorType, HttpStatusCode expectedStatus)
        {
            await using var factory = new JtmApiFactory();
            using HttpClient client = factory.CreateClient();
            var error = new Error("Tournaments.TestError", "The tournament request failed.", errorType);
            FakeTournamentService service = factory.FakeServices.TournamentService;
            service.CreateAsyncResult = Result<TournamentResponse>.Failure(error);
            service.GetByIdAsyncResult = Result<TournamentResponse>.Failure(error);
            using HttpRequestMessage request = CreateRequest(method);
            request.AddAuthenticatedUser();

            using HttpResponseMessage response = await client.SendAsync(request);

            response.StatusCode.Should().Be(expectedStatus);
            (await response.Content.ReadFromJsonAsync<Error>()).Should().Be(error);
            service.CreateAsyncCallCount.Should().Be(method == "POST" ? 1 : 0);
            service.GetByIdAsyncCallCount.Should().Be(method == "GET" ? 1 : 0);
        }

        [Fact]
        public async Task GetById_WithInvalidGuid_ReturnsNotFoundWithoutCallingService()
        {
            await using var factory = new JtmApiFactory();
            using HttpClient client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tournaments/not-a-guid");
            request.AddAuthenticatedUser();

            using HttpResponseMessage response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            factory.FakeServices.TournamentService.GetByIdAsyncCallCount.Should().Be(0);
        }

        [Fact]
        public async Task Create_WithMissingConfig_ReturnsBadRequestWithoutCallingService()
        {
            await using var factory = new JtmApiFactory();
            using HttpClient client = factory.CreateClient();
            CreateTournamentRequest arranged = TournamentsControllerTestData.CreateRequest();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tournaments")
            {
                Content = JsonContent.Create(new { arranged.Name, arranged.Location, arranged.Date })
            };
            request.AddAuthenticatedUser();

            using HttpResponseMessage response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            factory.FakeServices.TournamentService.CreateAsyncCallCount.Should().Be(0);
        }

        private static HttpRequestMessage CreateRequest(string method) => method == "POST"
            ? new HttpRequestMessage(HttpMethod.Post, "/api/tournaments")
            {
                Content = JsonContent.Create(TournamentsControllerTestData.CreateRequest())
            }
            : new HttpRequestMessage(HttpMethod.Get, $"/api/tournaments/{Guid.NewGuid()}");
    }
}