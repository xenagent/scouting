using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scommon;
using Scouting.API.Resources.Votes.Commands;
using Scouting.API.Shared.Results;

namespace Scouting.API.Controllers;

[Route("api/votes")]
public class VotesController : BaseApiController
{
    private readonly IMediator _mediator;

    public VotesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [Authorize]
    public async Task<FeatureResultModel> Vote(VotePlayer.Request request, CancellationToken ct)
        => await _mediator.SendAsync(request, ct);
}
