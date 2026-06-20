namespace Contracts.Users;

public record UserDto(
    string Id,
    string Email,
    string UserName,
    string? FullName,
    IReadOnlyList<string> Roles,
    bool IsActive);

public record CreateUserRequest(
    string Email,
    string UserName,
    string Password,
    string? FullName,
    IReadOnlyList<string> Roles);

public record UpdateUserRequest(
    string? FullName,
    IReadOnlyList<string> Roles,
    bool IsActive);
