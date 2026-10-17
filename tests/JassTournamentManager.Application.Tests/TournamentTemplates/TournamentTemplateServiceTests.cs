using FluentAssertions;
using JassTournamentManager.Application.Auth;
using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.Tests.Auth;
using JassTournamentManager.Application.Tests.Common;
using JassTournamentManager.Application.Tests.TournamentConfigs;
using JassTournamentManager.Application.Tests.Users;
using JassTournamentManager.Application.TournamentConfigs;
using JassTournamentManager.Application.TournamentTemplates;
using JassTournamentManager.Contracts.TournamentConfigs;
using JassTournamentManager.Contracts.TournamentTemplates;
using JassTournamentManager.Domain.Entities;

namespace JassTournamentManager.Application.Tests.TournamentTemplates
{
    public class TournamentTemplateServiceTests
    {
        private readonly FakeCurrentUser _currentUser;
        private readonly FakeUnitOfWork _unitOfWork;
        private readonly FakeTournamentTemplateRepository _tournamentTemplateRepository;
        private readonly FakeUserRepository _userRepository;
        private readonly TournamentTemplateService _tournamentTemplateService;

        public TournamentTemplateServiceTests()
        {
            _currentUser = new FakeCurrentUser();
            _unitOfWork = new FakeUnitOfWork();
            _tournamentTemplateRepository = new FakeTournamentTemplateRepository();
            _userRepository = new FakeUserRepository();

            _tournamentTemplateService = new TournamentTemplateService(_currentUser, _tournamentTemplateRepository, _userRepository, _unitOfWork);
        }

        [Fact]
        public async Task CreateAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            CreateTournamentTemplateRequest? nullRequest = null;

            Func<Task> act = () => _tournamentTemplateService.CreateAsync(nullRequest!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CreateAsync_WithEmptyCurrentUserId_ReturnsUnauthorized()
        {
            _currentUser.UserId = Guid.Empty;
            CreateTournamentTemplateRequest request = new(
                TournamentTestData.CreateTournamentConfigDto(),
                TournamentTemplateTestData.CreateLocation());

            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.CreateAsync(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(AuthErrors.Unauthorized);
            _tournamentTemplateRepository.TournamentTemplates.Should().BeEmpty();
            _unitOfWork.SaveChangesCallCount.Should().Be(0);
        }

        [Fact]
        public async Task CreateAsync_WithNonExistentOrganizerId_ReturnsOrganizerNotFound()
        {
            _currentUser.UserId = Guid.NewGuid();
            CreateTournamentTemplateRequest request = TournamentTemplateTestData.CreateCreateTournamentTemplateRequest();

            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.CreateAsync(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(TournamentTemplateErrors.OrganizerNotFound);
            _tournamentTemplateRepository.TournamentTemplates.Should().BeEmpty();
            _unitOfWork.SaveChangesCallCount.Should().Be(0);
        }

        [Fact]
        public async Task CreateAsync_WithUserWithExistingTournamentTemplate_ReturnsAlreadyExists()
        {
            User user = UserTestData.CreateUser();
            await _userRepository.AddAsync(user, CancellationToken.None);
            _currentUser.UserId = user.Id;

            TournamentTemplate tournamentTemplate = TournamentTemplateTestData.CreateTournamentTemplate(user.Id);
            await _tournamentTemplateRepository.AddAsync(tournamentTemplate, CancellationToken.None);

            CreateTournamentTemplateRequest request = TournamentTemplateTestData.CreateCreateTournamentTemplateRequest();
            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.CreateAsync(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(TournamentTemplateErrors.AlreadyExists);
            _tournamentTemplateRepository.TournamentTemplates.Count.Should().Be(1);
            _unitOfWork.SaveChangesCallCount.Should().Be(0);
        }

        [Fact]
        public async Task CreateAsync_WithNullConfig_ReturnsInvalidInput()
        {
            User user = UserTestData.CreateUser();
            await _userRepository.AddAsync(user, CancellationToken.None);
            _currentUser.UserId = user.Id;

            CreateTournamentTemplateRequest request = TournamentTemplateTestData.CreateCreateTournamentTemplateRequestWithNullConfig(user.Id);
            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.CreateAsync(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(CommonErrors.InvalidInput);
            _tournamentTemplateRepository.TournamentTemplates.Count.Should().Be(0);
            _unitOfWork.SaveChangesCallCount.Should().Be(0);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidConfig_ReturnsInvalidInput()
        {
            User user = UserTestData.CreateUser();
            await _userRepository.AddAsync(user, CancellationToken.None);
            _currentUser.UserId = user.Id;

            TournamentConfigDto config = TournamentTestData.CreateTournamentConfigDto(0);
            CreateTournamentTemplateRequest request = TournamentTemplateTestData.CreateCreateTournamentTemplateRequest(config);
            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.CreateAsync(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(CommonErrors.InvalidInput);
            _tournamentTemplateRepository.TournamentTemplates.Count.Should().Be(0);
            _unitOfWork.SaveChangesCallCount.Should().Be(0);
        }

        [Fact]
        public async Task CreateAsync_WithTooLongLocation_ReturnsInvalidInput()
        {
            User user = UserTestData.CreateUser();
            await _userRepository.AddAsync(user, CancellationToken.None);
            _currentUser.UserId = user.Id;

            CreateTournamentTemplateRequest request = TournamentTemplateTestData.CreateCreateTournamentTemplateRequest(
                location: TournamentTemplateTestData.CreateTooLongLocation());
            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.CreateAsync(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(CommonErrors.InvalidInput);
            _tournamentTemplateRepository.TournamentTemplates.Count.Should().Be(0);
            _unitOfWork.SaveChangesCallCount.Should().Be(0);
        }

        [Fact]
        public async Task CreateAsync_WithValidInput_CreatesTournamentTemplate()
        {
            User user = UserTestData.CreateUser();
            await _userRepository.AddAsync(user, CancellationToken.None);
            _currentUser.UserId = user.Id;

            CreateTournamentTemplateRequest request = TournamentTemplateTestData.CreateCreateTournamentTemplateRequest();
            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _tournamentTemplateRepository.TournamentTemplates.Count.Should().Be(1);
            _unitOfWork.SaveChangesCallCount.Should().Be(1);
            result.Value.Config.Should().BeEquivalentTo(request.Config);
            result.Value.OrganizerId.Should().Be(user.Id);
            result.Value.Location.Should().Be(request.Location);
        }

        [Fact]
        public async Task GetByIdAsync_WithEmptyTournamentTemplateId_ReturnsInvalidInput()
        {
            Guid emptyId = Guid.Empty;
            _currentUser.UserId = Guid.NewGuid();

            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.GetByIdAsync(emptyId, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(CommonErrors.InvalidInput);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentTournamentTemplateId_ReturnsNotFound()
        {
            Guid nonExistentId = Guid.NewGuid();
            _currentUser.UserId = Guid.NewGuid();

            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.GetByIdAsync(nonExistentId, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(TournamentTemplateErrors.NotFound);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonOwnerAndNonSysAdminUser_ReturnsForbidden()
        {
            TournamentTemplate tournamentTemplate = TournamentTemplateTestData.CreateTournamentTemplate();
            await _tournamentTemplateRepository.AddAsync(tournamentTemplate, CancellationToken.None);
            _currentUser.UserId = Guid.NewGuid();

            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.GetByIdAsync(tournamentTemplate.Id, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(CommonErrors.Forbidden);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistentTournamentTemplateId_ReturnsTournamentTemplate()
        {
            User user = UserTestData.CreateUser();
            _currentUser.UserId = user.Id;
            TournamentTemplate tournamentTemplate = TournamentTemplateTestData.CreateTournamentTemplate(user.Id);
            await _tournamentTemplateRepository.AddAsync(tournamentTemplate, CancellationToken.None);

            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.GetByIdAsync(tournamentTemplate.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(tournamentTemplate.Id);
            result.Value.OrganizerId.Should().Be(tournamentTemplate.OrganizerId);
            result.Value.Config.Should().BeEquivalentTo(TournamentConfigDtoMapper.ToDto(tournamentTemplate.ConfigValues));
            result.Value.Location.Should().Be(tournamentTemplate.Location);
        }

        [Fact]
        public async Task GetForCurrentUserAsync_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            _currentUser.IsAuthenticated = false;

            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.GetForCurrentUserAsync(CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(AuthErrors.Unauthorized);
        }

        [Fact]
        public async Task GetForCurrentUserAsync_WithNonExistentTournamentTemplateForUserId_ReturnsNotFound()
        {
            _currentUser.UserId = Guid.NewGuid();

            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.GetForCurrentUserAsync(CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(TournamentTemplateErrors.NotFound);
        }

        [Fact]
        public async Task GetForCurrentUserAsync_WithExistentTournamentTemplateForUserId_ReturnTournamentTemplate()
        {
            User user = UserTestData.CreateUser();
            _currentUser.UserId = user.Id;
            TournamentTemplate tournamentTemplate = TournamentTemplateTestData.CreateTournamentTemplate(user.Id);
            await _tournamentTemplateRepository.AddAsync(tournamentTemplate, CancellationToken.None);

            Result<TournamentTemplateResponse> result = await _tournamentTemplateService.GetForCurrentUserAsync(CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(tournamentTemplate.Id);
            result.Value.OrganizerId.Should().Be(user.Id);
            result.Value.Config.Should().BeEquivalentTo(TournamentConfigDtoMapper.ToDto(tournamentTemplate.ConfigValues));
            result.Value.Location.Should().Be(tournamentTemplate.Location);
        }
    }
}
