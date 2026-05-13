// GraphQL mutation resolver for managing reference data: log levels and employee positions.
using System.Text.Json;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.GraphQL.Types;
using Server.Models;

namespace Server.GraphQL.Mutations;

[GraphQLName("PermissionInput")]
public class PermissionInput
{
    public bool ToRead   { get; set; }
    public bool ToWrite  { get; set; }
    public bool ToDelete { get; set; }
    public bool ToUpdate { get; set; }
}

[GraphQLName("CreatePositionInput")]
public class CreatePositionInput
{
    public string Title { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public PermissionInput Permissions { get; set; } = new();
}

[GraphQLName("UpdatePositionInput")]
public class UpdatePositionInput
{
    public int PositionId { get; set; }
    public string? Title { get; set; }
    public decimal? Salary { get; set; }
    public PermissionInput? Permissions { get; set; }
}

[ExtendObjectType<Mutation>]
public class ReferenceMutationResolver
{
    [GraphQLName("create_log_level")]
    [Authorize]
    public async Task<LevelOfLogDto> CreateLogLevel(
        string levelName,
        string color,
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var trimName = levelName.Trim();
        var existing = await db.LevelOfLogs
            .FirstOrDefaultAsync(l => l.LevelName == trimName, cancellationToken);
        if (existing != null)
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Уровень «{trimName}» уже существует.")
                    .SetCode("DUPLICATE")
                    .Build());

        var level = new LevelOfLog { LevelName = trimName, Color = color.Trim() };
        db.LevelOfLogs.Add(level);
        await db.SaveChangesAsync(cancellationToken);

        return new LevelOfLogDto
        {
            LevelId   = level.LevelId.ToString(),
            LevelName = level.LevelName,
            Color     = level.Color
        };
    }

    [GraphQLName("update_log_level")]
    [Authorize]
    public async Task<LevelOfLogDto> UpdateLogLevel(
        int levelId,
        string levelName,
        string color,
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var level = await db.LevelOfLogs
            .FirstOrDefaultAsync(l => l.LevelId == levelId, cancellationToken)
            ?? throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Уровень не найден.").SetCode("NOT_FOUND").Build());

        level.LevelName = levelName.Trim();
        level.Color     = color.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return new LevelOfLogDto
        {
            LevelId   = level.LevelId.ToString(),
            LevelName = level.LevelName,
            Color     = level.Color
        };
    }

    [GraphQLName("delete_log_level")]
    [Authorize]
    public async Task<bool> DeleteLogLevel(
        int levelId,
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var level = await db.LevelOfLogs
            .FirstOrDefaultAsync(l => l.LevelId == levelId, cancellationToken);
        if (level == null) return false;

        db.LevelOfLogs.Remove(level);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    [GraphQLName("create_position")]
    [Authorize]
    public async Task<PositionDto> CreatePosition(
        CreatePositionInput input,
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var position = new Position
        {
            Title       = input.Title.Trim(),
            Salary      = input.Salary,
            Permissions = SerializePermissions(input.Permissions)
        };
        db.Positions.Add(position);
        await db.SaveChangesAsync(cancellationToken);
        return Querries.SystemQueryResolver.MapPosition(position);
    }

    [GraphQLName("update_position")]
    [Authorize]
    public async Task<PositionDto> UpdatePosition(
        UpdatePositionInput input,
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var position = await db.Positions
            .FirstOrDefaultAsync(p => p.PositionId == input.PositionId, cancellationToken)
            ?? throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Должность не найдена.").SetCode("NOT_FOUND").Build());

        if (input.Title != null)       position.Title  = input.Title.Trim();
        if (input.Salary.HasValue)     position.Salary = input.Salary.Value;
        if (input.Permissions != null) position.Permissions = SerializePermissions(input.Permissions);

        await db.SaveChangesAsync(cancellationToken);
        return Querries.SystemQueryResolver.MapPosition(position);
    }

    [GraphQLName("delete_position")]
    [Authorize]
    public async Task<bool> DeletePosition(
        int positionId,
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var position = await db.Positions
            .FirstOrDefaultAsync(p => p.PositionId == positionId, cancellationToken);
        if (position == null) return false;

        db.Positions.Remove(position);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string SerializePermissions(PermissionInput p) =>
        JsonSerializer.Serialize(new
        {
            toRead   = p.ToRead,
            toWrite  = p.ToWrite,
            toDelete = p.ToDelete,
            toUpdate = p.ToUpdate
        });
}
