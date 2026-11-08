using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RepairShop.Api.Auth;
using RepairShop.Application.Auth;

namespace RepairShop.Api.Controllers;

/// <summary>Sign-in. The only API endpoint that works without a token.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Exchanges user name and password for a bearer token.</summary>
    /// <param name="request">Credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The token, its expiry, user name and role.</response>
    /// <response code="401">Wrong user name or password — deliberately not saying which.</response>
    /// <response code="429">Too many attempts from this address; retry after a minute.</response>
    [HttpPost("token")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthSetup.LoginRateLimit)]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TokenResponseDto>> Token(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var token = await auth.LoginAsync(request, cancellationToken);
        return token is not null
            ? Ok(token)
            : Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Sign-in failed", detail: "User name or password is wrong.");
    }
}
