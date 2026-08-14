namespace Identity.Api.Models;

/// <summary>
/// A simple application user used by the password grant.
/// For the scaffold the user store is in-memory; swap for a real store (e.g. ASP.NET Core Identity)
/// when wiring persistent accounts.
/// </summary>
public sealed record ApplicationUser(
    int Id,
    string Username,
    string Email,
    string Password,
    string Role);
