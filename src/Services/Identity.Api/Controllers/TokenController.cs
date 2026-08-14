using System.Security.Claims;
using Identity.Api.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Identity.Api.Controllers;

/// <summary>
/// OAuth 2.0 / OIDC token endpoint (<c>/connect/token</c>).
/// Implements the password grant (validates end-user credentials) and the
/// client credentials grant (machine-to-machine).
/// </summary>
public sealed class TokenController(InMemoryUserStore users) : Controller
{
    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            var user = users.FindByUsername(request.Username);
            if (user is null || !users.VerifyCredentials(request.Username, request.Password))
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidGrant,
                    ErrorDescription = "Invalid username or password.",
                });
            }

            // Note: destinations must be set per-claim (via SetDestinations). The 3-argument
            // AddClaim(identity, type, value, string) overload binds its 4th argument as the
            // claim *issuer*, not as a destination, so claims without an explicit access-token
            // destination are dropped from the JWT by the OpenIddict server.
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString())
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.Username)
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email)
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, user.Role)
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken));

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes("profile", "email", "roles");

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsClientCredentialsGrantType())
        {
            // The client has already been authenticated by OpenIddict using its client_id/client_secret.
            // Return a token scoped to the calling application. The client_id is guaranteed present
            // at this point because OpenIddict only routes authenticated requests here.
            var subject = request.ClientId!;
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, subject)
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, subject)
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken));

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes("roles");

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new NotImplementedException("The specified grant type is not implemented.");
    }
}
