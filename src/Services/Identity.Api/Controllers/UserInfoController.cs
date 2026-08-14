using System.Security.Claims;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace Identity.Api.Controllers;

/// <summary>
/// OIDC userinfo endpoint (<c>/connect/userinfo</c>). Returns the claims of the
/// subject authenticated by the access token presented via the Authorization header.
/// </summary>
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[ApiController]
public sealed class UserInfoController(InMemoryUserStore users) : ControllerBase
{
    [HttpGet("~/connect/userinfo")]
    [Produces("application/json")]
    public IActionResult Userinfo()
    {
        var subject = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        var user = subject is null ? null : users.FindById(subject);

        if (user is null)
        {
            return Challenge(
                authenticationSchemes: OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictConstants.Parameters.Error] = OpenIddictConstants.Errors.InvalidToken,
                    [OpenIddictConstants.Parameters.ErrorDescription] = "The access token is invalid.",
                }));
        }

        return Ok(new Dictionary<string, object>
        {
            [OpenIddictConstants.Claims.Subject] = user.Id,
            [OpenIddictConstants.Claims.Name] = user.Username,
            [OpenIddictConstants.Claims.Email] = user.Email,
            [OpenIddictConstants.Claims.Role] = user.Role,
        });
    }
}
