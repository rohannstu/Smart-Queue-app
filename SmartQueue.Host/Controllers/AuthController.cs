using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartQueue.Application.Dtos;
using SmartQueue.Application.Interfaces;
using SmartQueue.Infrastructure.Interfaces;

namespace SmartQueue.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITenantProvider _tenantProvider;

    public AuthController(IAuthService authService, ITenantProvider tenantProvider)
    {
        _authService = authService;
        _tenantProvider = tenantProvider;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var organizationId = _tenantProvider.GetOrganizationId();
        if (organizationId == Guid.Empty)
        {
            return BadRequest("Organization context is required.");
        }

        var (success, error, response) = await _authService.LoginAsync(request.Email, request.Password, organizationId, cancellationToken);

        if (!success)
        {
            return Unauthorized(new { message = error });
        }

        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, error, response) = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (!success)
        {
            return Unauthorized(new { message = error });
        }

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.LogoutAsync(request.RefreshToken, cancellationToken);

        if (!success)
        {
            return BadRequest(new { message = "Logout failed." });
        }

        return Ok(new { message = "Logged out successfully." });
    }
}
