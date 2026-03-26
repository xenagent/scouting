using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scommon;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Resources.Players.Commands;
using Scouting.API.Resources.Players.Queries;
using Scouting.API.Shared.Results;

namespace Scouting.API.Controllers;

[Route("api/players")]
public class PlayersController : BaseApiController
{
    private readonly IMediator _mediator;

    public PlayersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<FeatureListResultModel<GetPlayerList.Response>> GetList(
        [FromQuery] string? search,
        [FromQuery] PlayerPosition? position,
        [FromQuery] string? league,
        [FromQuery] string? country,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => await _mediator.FetchAsync(new GetPlayerList.Request
        {
            Search = search,
            Position = position,
            League = league,
            Country = country,
            Page = page,
            PageSize = pageSize
        }, ct);

    [HttpGet("top")]
    public async Task<FeatureListResultModel<GetTopPlayers.Response>> GetTop(
        [FromQuery] int count = 10, CancellationToken ct = default)
        => await _mediator.FetchAsync(new GetTopPlayers.Request { Count = count }, ct);

    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<FeatureListResultModel<GetPendingPlayers.Response>> GetPending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => await _mediator.FetchAsync(new GetPendingPlayers.Request { Page = page, PageSize = pageSize }, ct);

    [HttpGet("{slug}")]
    public async Task<FeatureObjectResultModel<GetPlayerDetail.Response>> GetDetail(
        string slug, CancellationToken ct)
        => await _mediator.FetchAsync(new GetPlayerDetail.Request { Slug = slug }, ct);

    [HttpPost]
    [Authorize]
    public async Task<FeatureObjectResultModel<SuggestPlayer.Response>> Suggest(
        SuggestPlayer.Request request, CancellationToken ct)
        => await _mediator.SendAsync(request, ct);

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<FeatureResultModel> Approve(Guid id, CancellationToken ct)
        => await _mediator.SendAsync(new ApprovePlayer.Request { PlayerId = id }, ct);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<FeatureResultModel> Reject(Guid id, [FromBody] RejectPlayer.Request request, CancellationToken ct)
    {
        request.PlayerId = id;
        return await _mediator.SendAsync(request, ct);
    }
}
