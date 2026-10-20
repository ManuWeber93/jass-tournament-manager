using JassTournamentManager.Api.Common;
using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.Tournaments;
using JassTournamentManager.Contracts.Tournaments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JassTournamentManager.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tournaments")]
    public sealed class TournamentsController : ControllerBase
    {
        private readonly ITournamentService _tournamentService;

        public TournamentsController(ITournamentService tournamentService)
        {
            _tournamentService = tournamentService ?? throw new ArgumentNullException(nameof(tournamentService));
        }

        [HttpPost]
        public async Task<ActionResult<TournamentResponse>> CreateAsync(CreateTournamentRequest request, CancellationToken cancellationToken)
        {
            Result<TournamentResponse> result = await _tournamentService.CreateAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                return CreatedAtAction("GetById", new { id = result.Value.Id }, result.Value);
            }

            return this.ToActionResult(result.Error);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TournamentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Result<TournamentResponse> result = await _tournamentService.GetByIdAsync(id, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return this.ToActionResult(result.Error);
        }
    }
}