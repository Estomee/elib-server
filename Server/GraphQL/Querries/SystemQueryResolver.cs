// GraphQL query resolver for system administration: logs, stats, employees, positions, and log levels.
using System.Text.Json;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server;
using Server.Data;
using Server.GraphQL.Inputs;
using Server.GraphQL.Types;
using Server.Models;
using Server.Services;

namespace Server.GraphQL.Querries;

[ExtendObjectType<Query>]
public class SystemQueryResolver
{
    private static string EncodeCursor(long id) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(id.ToString()));

    private static long? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return long.TryParse(decoded, out var id) ? id : null;
        }
        catch { return null; }
    }

    [GraphQLName("system_logs")]
    [Authorize]
    public async Task<SystemLogsConnection> GetSystemLogs(
        [Service] ElibDbContext db,
        [Service] ICryptoService crypto,
        PaginationInput? pagination = null,
        LogFilterInput? filter = null,
        CancellationToken cancellationToken = default)
    {
        var afterId    = DecodeCursor(pagination?.After);
        var beforeId   = DecodeCursor(pagination?.Before);
        var isBackward = pagination?.Last.HasValue == true && beforeId.HasValue;
        var pageSize   = isBackward ? pagination!.Last!.Value : pagination?.First ?? 50;

        var query = db.SystemLogs
            .Include(l => l.Level)
            .Include(l => l.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter?.Action))
            query = query.Where(l => l.Action.Contains(filter.Action));

        if (!string.IsNullOrEmpty(filter?.LevelName))
            query = query.Where(l => l.Level.LevelName == filter.LevelName);

        if (!string.IsNullOrEmpty(filter?.UserEmail))
            query = query.Where(l => l.User != null && l.User.Email.Contains(filter.UserEmail));

        if (filter?.From.HasValue == true)
            query = query.Where(l => l.Timestamp >= filter.From);

        if (filter?.To.HasValue == true)
            query = query.Where(l => l.Timestamp <= filter.To);

        var totalCount = await query.CountAsync(cancellationToken);

        List<SystemLog> logs;
        if (isBackward)
        {
            query = query.Where(l => l.LogId < beforeId!.Value);
            logs = await query.OrderByDescending(l => l.LogId).Take(pageSize + 1).ToListAsync(cancellationToken);
            logs.Reverse();
        }
        else
        {
            if (afterId.HasValue) query = query.Where(l => l.LogId > afterId.Value);
            logs = await query.OrderBy(l => l.LogId).Take(pageSize + 1).ToListAsync(cancellationToken);
        }

        var hasExtra = logs.Count > pageSize;
        if (hasExtra) logs.RemoveAt(isBackward ? 0 : logs.Count - 1);

        var edges = logs.Select(l => new SystemLogEdge
        {
            Cursor = EncodeCursor(l.LogId),
            Node   = MapLog(l, crypto)
        }).ToList();

        return new SystemLogsConnection
        {
            TotalCount = totalCount,
            PageSize   = pageSize,
            Nodes      = edges.Select(e => e.Node).ToList(),
            Edges      = edges,
            PageInfo   = new SystemLogPageInfo
            {
                StartCursor     = edges.FirstOrDefault()?.Cursor,
                EndCursor       = edges.LastOrDefault()?.Cursor,
                HasNextPage     = isBackward ? beforeId.HasValue : hasExtra,
                HasPreviousPage = isBackward ? hasExtra : afterId.HasValue
            }
        };
    }

    [GraphQLName("admin_stats")]
    [Authorize]
    public async Task<AdminStatsDto> GetAdminStats(
        [Service] ElibDbContext db,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Database
            .SqlQuery<AdminStatsRaw>($"""
                SELECT
                    (SELECT COUNT(*)::int FROM "BOOKS")                             AS "BooksCount",
                    (SELECT COUNT(*)::int FROM "USERS" WHERE NOT employee)          AS "UsersCount",
                    (SELECT COUNT(*)::int FROM "USER_BOOKS" WHERE status='reading') AS "ReadingNow",
                    (SELECT COUNT(*)::int FROM "BOOKS"
                        WHERE created_at::date >= CURRENT_DATE)                     AS "UploadedToday"
                """)
            .FirstOrDefaultAsync(cancellationToken);

        return new AdminStatsDto
        {
            BooksCount    = row?.BooksCount    ?? 0,
            UsersCount    = row?.UsersCount    ?? 0,
            ReadingNow    = row?.ReadingNow    ?? 0,
            UploadedToday = row?.UploadedToday ?? 0
        };
    }

    private sealed record AdminStatsRaw(int BooksCount, int UsersCount, int ReadingNow, int UploadedToday);

    [GraphQLName("log_levels")]
    [Authorize]
    public async Task<List<LevelOfLogDto>> GetLogLevels(
        [Service] ElibDbContext db,
        CancellationToken cancellationToken = default)
    {
        var levels = await db.LevelOfLogs.OrderBy(l => l.LevelId).ToListAsync(cancellationToken);
        return levels.Select(l => new LevelOfLogDto
        {
            LevelId   = l.LevelId.ToString(),
            LevelName = l.LevelName,
            Color     = l.Color
        }).ToList();
    }

    [GraphQLName("employees")]
    [Authorize]
    public async Task<List<EmployeeDto>> GetEmployees(
        [Service] ElibDbContext db,
        CancellationToken cancellationToken = default)
    {
        var employees = await db.Employees
            .Include(e => e.User)
            .Include(e => e.Position)
            .OrderBy(e => e.EmployeeId)
            .ToListAsync(cancellationToken);

        return employees.Select(e => new EmployeeDto
        {
            EmployeeId      = e.EmployeeId,
            User            = AuthService.MapUser(e.User),
            Position        = MapPosition(e.Position),
            HireDate        = e.HireDate,
            TerminationDate = e.TerminationDate,
            PassportNumber  = e.PassportNumber ?? "",
            PassportSeries  = e.PassportSeries ?? ""
        }).ToList();
    }

    [GraphQLName("my_employee_role")]
    [Authorize]
    public async Task<string?> GetMyEmployeeRole(
        [Service] ElibDbContext db,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken = default)
    {
        var claim = http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (claim == null || !int.TryParse(claim, out var userId)) return null;

        var employee = await db.Employees
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        return employee?.Position?.Title;
    }

    [GraphQLName("positions")]
    [Authorize]
    public async Task<List<PositionDto>> GetPositions(
        [Service] ElibDbContext db,
        CancellationToken cancellationToken = default)
    {
        var positions = await db.Positions.OrderBy(p => p.PositionId).ToListAsync(cancellationToken);
        return positions.Select(MapPosition).ToList();
    }

    public static PositionDto MapPosition(Position? p)
    {
        if (p == null) return new PositionDto();
        var perms = new PermissionDto();
        try
        {
            var opts   = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<PermissionJson>(p.Permissions ?? "{}", opts);
            if (parsed != null)
            {
                perms.ToRead   = parsed.ToRead;
                perms.ToWrite  = parsed.ToWrite;
                perms.ToDelete = parsed.ToDelete;
                perms.ToUpdate = parsed.ToUpdate;
            }
        }
        catch { }
        return new PositionDto { PositionId = p.PositionId, Title = p.Title, Salary = p.Salary, Permissions = perms };
    }

    private record PermissionJson(bool ToRead = false, bool ToWrite = false, bool ToDelete = false, bool ToUpdate = false);

    private static SystemLogDto MapLog(SystemLog l, ICryptoService crypto) => new()
    {
        LogId     = l.LogId.ToString(),
        Timestamp = l.Timestamp ?? DateTime.UtcNow,
        Level     = new LevelOfLogDto { LevelId = l.LevelId.ToString(), LevelName = l.Level?.LevelName ?? "", Color = l.Level?.Color ?? "" },
        UserId    = l.UserId,
        Action    = l.Action,
        IpAddress = l.IpAddress != null ? crypto.TryDecrypt(l.IpAddress) : null
    };
}

public class SystemLogsConnection
{
    public List<SystemLogEdge> Edges { get; set; } = new();
    public List<SystemLogDto> Nodes { get; set; } = new();
    public int TotalCount { get; set; }
    public SystemLogPageInfo PageInfo { get; set; } = new();
    public int PageSize { get; set; }
}

public class SystemLogEdge
{
    public string Cursor { get; set; } = string.Empty;
    public SystemLogDto Node { get; set; } = new();
}

public class SystemLogPageInfo
{
    public string? StartCursor { get; set; }
    public string? EndCursor { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
