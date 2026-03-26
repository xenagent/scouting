using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scommon;
using Scouting.API.Resources.Follows.Commands;
using Scouting.API.Resources.Follows.Queries;
using Scouting.API.Resources.Scouters.Queries;
using Scouting.API.Services;
using Scouting.API.Shared.Results;

namespace Scouting.API.Controllers;

[Route("api/scouters")]
public class ScoutersController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ScoutersController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("top")]
    public async Task<FeatureListResultModel<GetTopScouters.Response>> GetTop(
        [FromQuery] int count = 10, CancellationToken ct = default)
        => await _mediator.FetchAsync(new GetTopScouters.Request { Count = count }, ct);

    [HttpGet("{username}")]
    public async Task<FeatureObjectResultModel<GetScouterProfile.Response>> GetProfile(
        string username, CancellationToken ct)
        => await _mediator.FetchAsync(new GetScouterProfile.Request
        {
            Username = username,
            ViewerUserId = _currentUser.IsAuthenticated ? _currentUser.Id : null
        }, ct);

    [HttpPost("{scouterId:guid}/follow")]
    [Authorize]
    public async Task<FeatureResultModel> Follow(Guid scouterId, CancellationToken ct)
        => await _mediator.SendAsync(new FollowScouter.Request { ScouterId = scouterId }, ct);

    [HttpDelete("{scouterId:guid}/follow")]
    [Authorize]
    public async Task<FeatureResultModel> Unfollow(Guid scouterId, CancellationToken ct)
        => await _mediator.SendAsync(new UnfollowScouter.Request { ScouterId = scouterId }, ct);

    [HttpGet("me/following")]
    [Authorize]
    public async Task<FeatureListResultModel<GetMyFollowing.Response>> GetMyFollowing(CancellationToken ct)
        => await _mediator.FetchAsync(new GetMyFollowing.Request(), ct);
}
