using FluentAssertions;
using JassTournamentManager.Application.Auth;
using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.Tests.Auth;
using JassTournamentManager.Application.Tests.Users;
using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Contracts.TournamentConfigs;
using JassTournamentManager.Contracts.Tournaments;
using JassTournamentManager.Domain.Entities;
using JassTournamentManager.Domain.Enums;
using JassTournamentManager.Domain.Services;
using JassTournamentManager.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace JassTournamentManager.Application.Tests.Tournaments
{
    public class TournamentServiceTests
    {
        private readonly FakeCurrentUser _currentUser = new();
        private readonly FakeUserRepository _users = new();
        private readonly FakeTournamentRepository _repository = new();
        private readonly FakeTournamentCodeGenerator _codes = new();
        private readonly FakeTournamentUnitOfWork _unitOfWork = new();
        private readonly TournamentService _service;

        public TournamentServiceTests()
        {
            _service = new(_currentUser, _users, _repository, _codes, _unitOfWork);
        }

        [Fact]
        public async Task CreateAsync_WithExplicitConfiguration_PersistsPendingRoundsAndMapsEveryField()
        {
            await ArrangeOrganizerAsync();
            CreateTournamentRequest request = CreateRequest();

            Result<TournamentResponse> result = await _service.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            Tournament tournament = _repository.Tournaments.Should().ContainSingle().Subject;
            tournament.OrganizerId.Should().Be(_currentUser.UserId);
            tournament.Details.Name.Should().Be(request.Name);
            tournament.Details.Location.Should().Be(request.Location);
            tournament.Details.Date.Should().Be(request.Date);
            tournament.Details.TournamentCode.Should().Be(_codes.GeneratedCodes.Single());
            tournament.ConfigValues.Should().BeEquivalentTo(request.Config);
            tournament.Status.Should().Be(TournamentStatus.Active);
            var organizer = tournament.Participants.Should().ContainSingle().Subject;
            organizer.UserId.Should().Be(_currentUser.UserId);
            organizer.TournamentId.Should().Be(tournament.Id);
            organizer.Role.Should().Be(ParticipantRole.Organizer);
            organizer.RegistrationMethod.Should().Be(RegistrationMethod.ByOrganizer);
            organizer.IsPlaying.Should().BeTrue();
            tournament.Rounds.Select(round => round.RoundNumber).Should().Equal(Enumerable.Range(1, request.Config.NumberOfRounds));
            tournament.Rounds.Should().OnlyContain(round => round.TournamentId == tournament.Id && round.Status == RoundStatus.Pending);
            tournament.Rounds.Should().OnlyContain(round => round.Pairings.Count == 0);
            AssertResponse(result.Value, tournament, request.Config);
            _repository.AddCallCount.Should().Be(1);
            _unitOfWork.SaveChangesCallCount.Should().Be(1);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public async Task CreateAndGet_WithUnauthenticatedOrEmptyUser_ReturnUnauthorized(bool authenticated, bool emptyId)
        {
            _currentUser.IsAuthenticated = authenticated;
            _currentUser.UserId = emptyId ? Guid.Empty : Guid.NewGuid();

            var create = await _service.CreateAsync(CreateRequest(), CancellationToken.None);
            var get = await _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

            create.Error.Should().Be(AuthErrors.Unauthorized);
            get.Error.Should().Be(AuthErrors.Unauthorized);
            AssertNoWrite();
            _repository.GetCallCount.Should().Be(0);
            _codes.GeneratedCodes.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("  Village Hall  ")]
        public async Task CreateAsync_WithOptionalLocation_NormalizesDetails(string? location)
        {
            await ArrangeOrganizerAsync();
            CreateTournamentRequest request = CreateRequest() with { Name = "  Summer Cup  ", Location = location };

            var result = await _service.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be(request.Name.Trim());
            result.Value.Location.Should().Be(request.Location?.Trim());
        }

        [Fact]
        public async Task CreateAsync_WithMissingOrganizer_ReturnsOrganizerNotFound()
        {
            _currentUser.UserId = Guid.NewGuid();
            var result = await _service.CreateAsync(CreateRequest(), CancellationToken.None);
            result.Error.Should().Be(TournamentErrors.OrganizerNotFound);
            AssertNoWrite();
            _codes.GeneratedCodes.Should().BeEmpty();
        }

        public static IEnumerable<object[]> InvalidRequests()
        {
            CreateTournamentRequest request = CreateRequest();
            yield return [request with { Name = null! }];
            yield return [request with { Name = "" }];
            yield return [request with { Name = "   " }];
            yield return [request with { Name = new string('N', 201) }];
            yield return [request with { Location = new string('L', 201) }];
            yield return [request with { Date = default }];
            yield return [request with { Config = null! }];
            yield return [request with { Config = request.Config with { NumberOfRounds = 51 } }];
            yield return [request with { Config = request.Config with { NumberOfRounds = int.MaxValue } }];
            foreach (int invalidValue in new[] { 0, -1 })
            {
                yield return [request with { Config = request.Config with { NumberOfRounds = invalidValue } }];
                yield return [request with { Config = request.Config with { GamesPerRound = invalidValue } }];
            }
            yield return [request with { Config = request.Config with { ScoreVisibility = (ScoreVisibility)(-1) } }];
            yield return [request with { Config = request.Config with { ScoreVisibility = (ScoreVisibility)99 } }];
        }

        [Theory]
        [MemberData(nameof(InvalidRequests))]
        public async Task CreateAsync_WithInvalidRequest_ReturnsInvalidInputWithoutWriting(CreateTournamentRequest request)
        {
            await ArrangeOrganizerAsync();
            var result = await _service.CreateAsync(request, CancellationToken.None);
            result.Error.Should().Be(CommonErrors.InvalidInput);
            AssertNoWrite();
        }

        [Fact]
        public async Task CreateAsync_WithFiftyRounds_CreatesAllRounds()
        {
            await ArrangeOrganizerAsync();
            var request = CreateRequest();
            request = request with { Config = request.Config with { NumberOfRounds = 50 } };

            var result = await _service.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Rounds.Should().HaveCount(request.Config.NumberOfRounds);
            result.Value.Config.Should().Be(request.Config);
            _unitOfWork.SaveChangesCallCount.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _service.CreateAsync(null!, CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
            AssertNoWrite();
        }

        [Theory]
        [InlineData("organizer")]
        [InlineData("participant")]
        [InlineData("sysadmin")]
        [InlineData("foreign")]
        public async Task GetByIdAsync_EnforcesAccessAndMapsResponse(string access)
        {
            Tournament tournament = ArrangeTournament();
            _currentUser.UserId = access == "organizer" ? tournament.OrganizerId : Guid.NewGuid();
            _currentUser.IsSysAdmin = access == "sysadmin";
            if (access == "participant")
            {
                tournament.AddParticipant(new TournamentParticipant(tournament.Id, _currentUser.UserId, default));
            }

            var result = await _service.GetByIdAsync(tournament.Id, CancellationToken.None);

            if (access == "foreign")
            {
                result.Error.Should().Be(CommonErrors.Forbidden);
            }
            else
            {
                result.IsSuccess.Should().BeTrue();
                AssertResponse(result.Value, tournament, CreateRequest().Config);
            }
            _unitOfWork.SaveChangesCallCount.Should().Be(0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetByIdAsync_WithEmptyOrMissingId_ReturnsExpectedError(bool emptyId)
        {
            _currentUser.UserId = Guid.NewGuid();
            var result = await _service.GetByIdAsync(emptyId ? Guid.Empty : Guid.NewGuid(), CancellationToken.None);
            result.Error.Should().Be(emptyId ? CommonErrors.InvalidInput : TournamentErrors.NotFound);
            _repository.GetCallCount.Should().Be(emptyId ? 0 : 1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CreateAsync_WithCodeCollisions_RetriesUntilFifthCandidateAndAddsOnce(bool saveRace)
        {
            await ArrangeOrganizerAsync();
            string[] candidates = _codes.Candidates.ToArray();
            List<Tournament> savedInstances = [];
            if (saveRace)
            {
                _unitOfWork.OnSave = attempt =>
                {
                    savedInstances.Add(_repository.Tournaments.Single());
                    if (attempt < 5)
                    {
                        throw CreateCodeConflict();
                    }
                };
            }
            else
            {
                _repository.OccupiedCodes.UnionWith(candidates.Take(4));
            }

            var result = await _service.CreateAsync(CreateRequest(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.TournamentCode.Should().Be(candidates[4]);
            _codes.GeneratedCodes.Should().Equal(candidates.Take(5));
            _repository.CheckedCodes.Should().Equal(candidates.Take(5));
            _repository.AddCallCount.Should().Be(1);
            _unitOfWork.SaveChangesCallCount.Should().Be(saveRace ? 5 : 1);
            savedInstances.Should().OnlyContain(tournament => ReferenceEquals(tournament, _repository.Tournaments.Single()));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CreateAsync_WhenAllFiveCandidatesCollide_ReturnsCodeGenerationFailed(bool saveRace)
        {
            await ArrangeOrganizerAsync();
            string[] candidates = _codes.Candidates.ToArray();
            if (saveRace)
            {
                _unitOfWork.OnSave = _ => throw CreateCodeConflict();
            }
            else
            {
                _repository.OccupiedCodes.UnionWith(candidates);
            }

            var result = await _service.CreateAsync(CreateRequest(), CancellationToken.None);

            result.Error.Should().Be(TournamentErrors.CodeGenerationFailed);
            _codes.GeneratedCodes.Should().Equal(candidates.Take(5));
            _repository.CheckedCodes.Should().Equal(candidates.Take(5));
            _repository.AddCallCount.Should().Be(saveRace ? 1 : 0);
            _unitOfWork.SaveChangesCallCount.Should().Be(saveRace ? 5 : 0);
        }

        [Fact]
        public async Task CreateAsync_WithMixedPrecheckAndSaveConflicts_KeepsTrackedTournamentAndSharedAttemptBudget()
        {
            await ArrangeOrganizerAsync();
            string[] candidates = _codes.Candidates.ToArray();
            _repository.OccupiedCodes.UnionWith([candidates[0], candidates[2], candidates[3]]);
            Tournament? firstSavedTournament = null;
            _unitOfWork.OnSave = attempt =>
            {
                Tournament tournament = _repository.Tournaments.Single();
                if (attempt == 1)
                {
                    firstSavedTournament = tournament;
                    throw CreateCodeConflict();
                }
                tournament.Should().BeSameAs(firstSavedTournament);
            };

            var result = await _service.CreateAsync(CreateRequest(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.TournamentCode.Should().Be(candidates[4]);
            _codes.GeneratedCodes.Should().Equal(candidates.Take(5));
            _repository.AddCallCount.Should().Be(1);
            _unitOfWork.SaveChangesCallCount.Should().Be(2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CreateAsync_WithUnrelatedDependencyException_PropagatesWithoutRetry(bool duringSave)
        {
            await ArrangeOrganizerAsync();
            InvalidOperationException exception = new("Unexpected persistence failure");
            if (duringSave)
            {
                _unitOfWork.OnSave = _ => throw exception;
            }
            else
            {
                _repository.Exception = exception;
            }

            Func<Task> act = () => _service.CreateAsync(CreateRequest(), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
            _codes.GeneratedCodes.Should().ContainSingle();
            _unitOfWork.SaveChangesCallCount.Should().Be(duringSave ? 1 : 0);
        }

        [Fact]
        public async Task CreateAndGet_WithCanceledToken_PropagateCancellationWithoutWriting()
        {
            await ArrangeOrganizerAsync();
            using CancellationTokenSource source = new();
            source.Cancel();
            Func<Task> create = () => _service.CreateAsync(CreateRequest(), source.Token);
            Func<Task> get = () => _service.GetByIdAsync(Guid.NewGuid(), source.Token);
            await create.Should().ThrowAsync<OperationCanceledException>();
            await get.Should().ThrowAsync<OperationCanceledException>();
            AssertNoWrite();
        }

        [Fact]
        public async Task CreateAsync_WhenCanceledAfterSaveConflict_DoesNotGenerateAnotherCandidate()
        {
            await ArrangeOrganizerAsync();
            using CancellationTokenSource source = new();
            _unitOfWork.OnSave = _ =>
            {
                source.Cancel();
                throw CreateCodeConflict();
            };
            Func<Task> act = () => _service.CreateAsync(CreateRequest(), source.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
            _codes.GeneratedCodes.Should().ContainSingle();
            _repository.AddCallCount.Should().Be(1);
            _unitOfWork.SaveChangesCallCount.Should().Be(1);
        }

        [Fact]
        public async Task CreateAndGet_ForwardCancellationTokenToDependencies()
        {
            await ArrangeOrganizerAsync();
            using CancellationTokenSource source = new();
            var result = await _service.CreateAsync(CreateRequest(), source.Token);
            await _service.GetByIdAsync(result.Value.Id, source.Token);
            _repository.ReceivedTokens.Should().HaveCount(3).And.OnlyContain(token => token == source.Token);
            _unitOfWork.ReceivedTokens.Should().Equal(source.Token);
        }

        [Theory]
        [InlineData("currentUser")]
        [InlineData("userRepository")]
        [InlineData("tournamentRepository")]
        [InlineData("codeGenerator")]
        [InlineData("unitOfWork")]
        public void Constructor_WithNullDependency_ThrowsArgumentNullException(string parameter)
        {
            Action act = () => new TournamentService(
                parameter == "currentUser" ? null! : _currentUser,
                parameter == "userRepository" ? null! : _users,
                parameter == "tournamentRepository" ? null! : _repository,
                parameter == "codeGenerator" ? null! : _codes,
                parameter == "unitOfWork" ? null! : _unitOfWork);
            act.Should().Throw<ArgumentNullException>().WithParameterName(parameter);
        }

        [Fact]
        public void AddApplication_RegistersTournamentServiceAndGeneratorWithExpectedLifetimes()
        {
            IServiceCollection services = new ServiceCollection().AddApplication();
            services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(ITournamentService)
                && descriptor.ImplementationType == typeof(TournamentService) && descriptor.Lifetime == ServiceLifetime.Scoped);
            services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(ITournamentCodeGenerator)
                && descriptor.ImplementationType == typeof(RandomTournamentCodeGenerator) && descriptor.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void RandomTournamentCodeGenerator_GeneratesCodesWithDomainAlphabet()
        {
            RandomTournamentCodeGenerator generator = new();

            string[] codes = Enumerable.Range(0, 100).Select(_ => generator.Generate()).ToArray();

            codes.Should().OnlyContain(code => code.Length == 6);
            codes.SelectMany(code => code).Should().OnlyContain(character => TournamentCodeGenerator.AllowedChars.Contains(character));
        }

        private async Task ArrangeOrganizerAsync()
        {
            User organizer = UserTestData.CreateUser();
            await _users.AddAsync(organizer, CancellationToken.None);
            _currentUser.UserId = organizer.Id;
        }

        private Tournament ArrangeTournament()
        {
            CreateTournamentRequest request = CreateRequest();
            Tournament tournament = TournamentFactory.Create(Guid.NewGuid(),
                new TournamentDetails(request.Name, request.Location, request.Date, "READ01"),
                new TournamentConfigValues(request.Config.NumberOfRounds, request.Config.GamesPerRound,
                    request.Config.MatchBonusEnabled, request.Config.IsFixedTeams, request.Config.ScoreVisibility));
            _repository.Tournaments.Add(tournament);
            return tournament;
        }

        private void AssertNoWrite()
        {
            _repository.AddCallCount.Should().Be(0);
            _unitOfWork.SaveChangesCallCount.Should().Be(0);
        }

        private static CreateTournamentRequest CreateRequest() => new("Autumn Cup", "Community Hall",
            new DateOnly(2027, 10, 24), new TournamentConfigDto(3, 6, false, true, ScoreVisibility.OrganizerOnly));

        private static TournamentCodeConflictException CreateCodeConflict() => new(new InvalidOperationException("Unique code conflict"));

        private static void AssertResponse(TournamentResponse response, Tournament tournament, TournamentConfigDto config)
        {
            response.Id.Should().Be(tournament.Id);
            response.OrganizerId.Should().Be(tournament.OrganizerId);
            response.Name.Should().Be(tournament.Details.Name);
            response.Location.Should().Be(tournament.Details.Location);
            response.Date.Should().Be(tournament.Details.Date);
            response.TournamentCode.Should().Be(tournament.Details.TournamentCode);
            response.Config.Should().BeEquivalentTo(config);
            response.Status.Should().Be(tournament.Status);
            response.CreatedAt.Should().Be(tournament.CreatedAt);
            response.UpdatedAt.Should().Be(tournament.UpdatedAt);
            response.Rounds.Should().Equal(tournament.Rounds.OrderBy(round => round.RoundNumber)
                .Select(round => new RoundResponse(round.Id, round.RoundNumber, round.Status)));
        }
    }
}