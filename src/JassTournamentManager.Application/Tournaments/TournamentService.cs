using JassTournamentManager.Application.Auth;
using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.TournamentConfigs;
using JassTournamentManager.Application.Users;
using JassTournamentManager.Contracts.Tournaments;
using JassTournamentManager.Domain.Entities;
using JassTournamentManager.Domain.Services;
using JassTournamentManager.Domain.ValueObjects;

namespace JassTournamentManager.Application.Tournaments
{
    public sealed class TournamentService : ITournamentService
    {
        private const int MaximumCodeAttempts = 5;
        private const int MaximumRoundsPerCreation = 50;
        private readonly ICurrentUser _currentUser;
        private readonly IUserRepository _userRepository;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ITournamentCodeGenerator _codeGenerator;
        private readonly IUnitOfWork _unitOfWork;

        public TournamentService(
            ICurrentUser currentUser,
            IUserRepository userRepository,
            ITournamentRepository tournamentRepository,
            ITournamentCodeGenerator codeGenerator,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        private bool IsCurrentUserAuthenticated => _currentUser.IsAuthenticated && _currentUser.UserId != Guid.Empty;

        public async Task<Result<TournamentResponse>> CreateAsync(CreateTournamentRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!IsCurrentUserAuthenticated)
            {
                return Result<TournamentResponse>.Failure(AuthErrors.Unauthorized);
            }

            if (!await _userRepository.ExistsAsync(_currentUser.UserId, cancellationToken))
            {
                return Result<TournamentResponse>.Failure(TournamentErrors.OrganizerNotFound);
            }

            Result<Tournament> creationResult = CreateTournament(request);
            if (creationResult.IsFailure)
            {
                return Result<TournamentResponse>.Failure(creationResult.Error);
            }

            return await SaveWithUniqueCodeAsync(creationResult.Value, cancellationToken);
        }

        public async Task<Result<TournamentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!IsCurrentUserAuthenticated)
            {
                return Result<TournamentResponse>.Failure(AuthErrors.Unauthorized);
            }

            if (id == Guid.Empty)
            {
                return Result<TournamentResponse>.Failure(CommonErrors.InvalidInput);
            }

            Tournament? tournament = await _tournamentRepository.GetByIdAsync(id, cancellationToken);
            if (tournament is null)
            {
                return Result<TournamentResponse>.Failure(TournamentErrors.NotFound);
            }

            if (!CanAccess(tournament))
            {
                return Result<TournamentResponse>.Failure(CommonErrors.Forbidden);
            }

            return Result<TournamentResponse>.Success(CreateResponse(tournament));
        }

        private Result<Tournament> CreateTournament(CreateTournamentRequest request)
        {
            try
            {
                TournamentConfigValues configValues = TournamentConfigDtoMapper.FromDto(request.Config);
                if (configValues.NumberOfRounds > MaximumRoundsPerCreation || !Enum.IsDefined(configValues.ScoreVisibility))
                {
                    return Result<Tournament>.Failure(CommonErrors.InvalidInput);
                }

                TournamentDetails details = new(request.Name, request.Location, request.Date, _codeGenerator.Generate());
                Tournament tournament = TournamentFactory.Create(_currentUser.UserId, details, configValues);
                return Result<Tournament>.Success(tournament);
            }
            catch (ArgumentException)
            {
                return Result<Tournament>.Failure(CommonErrors.InvalidInput);
            }
        }

        private async Task<Result<TournamentResponse>> SaveWithUniqueCodeAsync(Tournament tournament, CancellationToken cancellationToken)
        {
            bool added = false;
            for (int attempt = 0; attempt < MaximumCodeAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attempt > 0)
                {
                    tournament.UpdateDetails(tournament.Details with { TournamentCode = _codeGenerator.Generate() });
                }

                if (await _tournamentRepository.ExistsByCodeAsync(tournament.Details.TournamentCode, cancellationToken))
                {
                    continue;
                }

                if (!added)
                {
                    await _tournamentRepository.AddAsync(tournament, cancellationToken);
                    added = true;
                }

                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return Result<TournamentResponse>.Success(CreateResponse(tournament));
                }
                catch (TournamentCodeConflictException)
                {
                    // The unique index also protects against concurrent code allocation.
                }
            }

            return Result<TournamentResponse>.Failure(TournamentErrors.CodeGenerationFailed);
        }

        private bool CanAccess(Tournament tournament) =>
            _currentUser.IsSysAdmin ||
            tournament.OrganizerId == _currentUser.UserId ||
            tournament.Participants.Any(participant => participant.UserId == _currentUser.UserId);

        private static TournamentResponse CreateResponse(Tournament tournament) => new(
            tournament.Id,
            tournament.OrganizerId,
            tournament.Details.Name,
            tournament.Details.Location,
            tournament.Details.Date,
            tournament.Details.TournamentCode,
            TournamentConfigDtoMapper.ToDto(tournament.ConfigValues),
            tournament.Status,
            tournament.Rounds.OrderBy(round => round.RoundNumber)
                .Select(round => new RoundResponse(round.Id, round.RoundNumber, round.Status)).ToArray(),
            tournament.CreatedAt,
            tournament.UpdatedAt);
    }
}