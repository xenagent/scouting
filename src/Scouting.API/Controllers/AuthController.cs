using Microsoft.AspNetCore.Mvc;
using scommon;
using Scouting.API.Resources.Auth.Commands;
using Scouting.API.Shared.Results;

namespace Scouting.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<FeatureObjectResultModel<Register.Response>> Register(
        Register.Request request, CancellationToken ct)
        => await _mediator.SendAsync(request, ct);

    [HttpPost("login")]
    public async Task<FeatureObjectResultModel<Login.Response>> Login(
        Login.Request request, CancellationToken ct)
        => await _mediator.SendAsync(request, ct);
}
