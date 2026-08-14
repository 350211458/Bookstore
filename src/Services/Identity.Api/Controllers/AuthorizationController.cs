using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Identity.Api.Controllers;

/// <summary>
/// OIDC authorization endpoint (<c>/connect/authorize</c>).
/// The endpoint is registered and the authorization code flow enabled; an interactive
/// login/consent UI is out of scope for the scaffold and is added when the identity
/// service gets its sign-in pages.
/// </summary>
public sealed class AuthorizationController : Controller
{
    [HttpGet("~/connect/authorize")]
    public IActionResult Authorize()
    {
        _ = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.AccessDenied,
            ErrorDescription = "Interactive authorization (login/consent) is not implemented in this scaffold.",
        });
    }
}
