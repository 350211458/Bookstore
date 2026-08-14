using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using OpenIddict.Abstractions;

namespace Identity.Api.Tests;

/// <summary>
/// Integration tests for the OIDC token endpoint (<c>/connect/token</c>).
/// </summary>
public sealed class TokenEndpointTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    [Fact]
    public async Task PasswordFlow_ValidCredentials_ReturnsJwtAccessTokenWithExpectedClaims()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "bookstore-app",
            ["username"] = "alice",
            ["password"] = "P@ssw0rd!",
            ["scope"] = "profile email roles",
        }));

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}: {responseBody}");

        using var json = JsonDocument.Parse(responseBody);
        var token = json.RootElement.GetProperty("access_token").GetString();
        Assert.False(string.IsNullOrEmpty(token), "An access_token must be returned.");
        Assert.Equal("Bearer", json.RootElement.GetProperty("token_type").GetString());

        // The token must be a readable JWT carrying the sub, email and role claims.
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var allClaims = string.Join(" | ", jwt.Claims.Select(c => $"{c.Type}={c.Value}"));
        Assert.True(jwt.Claims.Any(c => c.Type == OpenIddictConstants.Claims.Subject), $"No 'sub' claim. Token claims: {allClaims}");
        Assert.True(jwt.Claims.Any(c => c.Type == OpenIddictConstants.Claims.Email), $"No 'email' claim. Token claims: {allClaims}");
        Assert.Equal("1", jwt.Claims.First(c => c.Type == OpenIddictConstants.Claims.Subject).Value);
        Assert.Equal("alice@example.com", jwt.Claims.First(c => c.Type == OpenIddictConstants.Claims.Email).Value);
        Assert.Equal("Customer", jwt.Claims.First(c => c.Type == OpenIddictConstants.Claims.Role).Value);
    }

    [Fact]
    public async Task PasswordFlow_InvalidCredentials_ReturnsInvalidGrant()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "bookstore-app",
            ["username"] = "alice",
            ["password"] = "wrong-password",
            ["scope"] = "profile email roles",
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PasswordFlow_UnknownUser_ReturnsInvalidGrant()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "bookstore-app",
            ["username"] = "nobody",
            ["password"] = "P@ssw0rd!",
            ["scope"] = "profile email roles",
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UserInfo_WithValidAccessToken_ReturnsUserClaims()
    {
        var client = factory.CreateClient();

        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "bookstore-app",
            ["username"] = "alice",
            ["password"] = "P@ssw0rd!",
            ["scope"] = "profile email roles",
        }));

        var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();

        var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new("Bearer", accessToken);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("1", json.RootElement.GetProperty(OpenIddictConstants.Claims.Subject).ToString());
        Assert.Equal("alice@example.com", json.RootElement.GetProperty(OpenIddictConstants.Claims.Email).GetString());
        Assert.Equal("Customer", json.RootElement.GetProperty(OpenIddictConstants.Claims.Role).GetString());
    }
}
