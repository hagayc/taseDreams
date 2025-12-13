using Microsoft.AspNetCore.Mvc;
using TaseDreams.Api.Services;

namespace TaseDreams.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILdapService _ldapService;

    public AuthController(ILdapService ldapService)
    {
        _ldapService = ldapService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<object>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username and password are required" });
        }

        var isAuthenticated = await _ldapService.AuthenticateAsync(request.Username, request.Password);
        
        if (!isAuthenticated)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var groups = await _ldapService.GetUserGroupsAsync(request.Username);

        return Ok(new
        {
            username = request.Username,
            groups = groups,
            authenticated = true
        });
    }

    [HttpPost("verify")]
    public async Task<ActionResult<object>> VerifyGroup([FromBody] VerifyGroupRequest request)
    {
        var isInGroup = await _ldapService.IsUserInGroupAsync(request.Username, request.GroupName);
        
        return Ok(new
        {
            username = request.Username,
            group = request.GroupName,
            isMember = isInGroup
        });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class VerifyGroupRequest
{
    public string Username { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
}

