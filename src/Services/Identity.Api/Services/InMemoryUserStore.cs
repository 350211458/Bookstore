using Identity.Api.Models;

namespace Identity.Api.Services;

/// <summary>
/// Development-only in-memory user store. Seeded with the users documented in the spec
/// (roles: Customer, Admin).
/// </summary>
public sealed class InMemoryUserStore
{
    private readonly IReadOnlyList<ApplicationUser> _users;

    public InMemoryUserStore()
    {
        _users =
        [
            new ApplicationUser(Id: 1, Username: "alice", Email: "alice@example.com", Password: "P@ssw0rd!", Role: "Customer"),
            new ApplicationUser(Id: 2, Username: "admin", Email: "admin@example.com", Password: "Admin@123", Role: "Admin"),
        ];
    }

    public ApplicationUser? FindByUsername(string? username) =>
        _users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.Ordinal));

    public ApplicationUser? FindById(string? subject) =>
        _users.FirstOrDefault(u => string.Equals(u.Id.ToString(), subject, StringComparison.Ordinal));

    public bool VerifyCredentials(string? username, string? password) =>
        FindByUsername(username) is { } user
        && string.Equals(user.Password, password, StringComparison.Ordinal);
}
