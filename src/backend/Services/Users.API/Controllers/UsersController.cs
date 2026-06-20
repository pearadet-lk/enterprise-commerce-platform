using Contracts.Audit;
using Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Users.API.Domain;
using Users.API.Infrastructure;

namespace Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(UsersDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await dbContext.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync();
        return Ok(users.Select(Map));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(Map(user));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        if (await dbContext.Users.AnyAsync(u => u.Email == request.Email || u.UserName == request.UserName))
        {
            return Conflict("A user with the same email or username already exists.");
        }

        var user = new UserProfile
        {
            Email = request.Email,
            UserName = request.UserName,
            FullName = request.FullName,
            Roles = string.Join(",", request.Roles),
            CreatedBy = User.Identity?.Name ?? "system"
        };

        dbContext.Users.Add(user);
        await dbContext.AuditLogs.AddAsync(CreateAudit("Create", "User", user.Id.ToString(), $"Created user {user.UserName}"));
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, Map(user));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, UpdateUserRequest request)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = request.FullName;
        user.Roles = string.Join(",", request.Roles);
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = User.Identity?.Name;

        await dbContext.AuditLogs.AddAsync(CreateAudit("Update", "User", user.Id.ToString(), $"Updated user {user.UserName}"));
        await dbContext.SaveChangesAsync();

        return Ok(Map(user));
    }

    private AuditLog CreateAudit(string action, string entityType, string? entityId, string details) =>
        new()
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = User.FindFirst("sub")?.Value ?? "unknown",
            UserName = User.Identity?.Name ?? "unknown",
            Details = details
        };

    private static UserDto Map(UserProfile user) =>
        new(
            user.Id.ToString(),
            user.Email,
            user.UserName,
            user.FullName,
            user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            user.IsActive);
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AuditLogsController(UsersDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs()
    {
        var logs = await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.Timestamp)
            .Take(200)
            .ToListAsync();

        return Ok(logs.Select(l => new AuditLogDto(
            l.Id,
            l.Action,
            l.EntityType,
            l.EntityId,
            l.UserId,
            l.UserName,
            l.Timestamp,
            l.Details)));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(UsersDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary()
    {
        var userCount = await dbContext.Users.CountAsync();
        var auditCount = await dbContext.AuditLogs.CountAsync();

        return Ok(new
        {
            Users = userCount,
            AuditLogs = auditCount,
            GeneratedAt = DateTime.UtcNow
        });
    }
}
