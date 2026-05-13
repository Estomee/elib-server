// Service that writes system log entries to the database with encrypted IP addresses and action-level lookup.
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services;

public interface ILogService
{
    Task LogAsync(int? userId, string action, string? ip, CancellationToken ct = default);
}

public class LogService : ILogService
{
    private readonly IDbContextFactory<ElibDbContext> _dbFactory;
    private readonly ICryptoService _crypto;

    public LogService(IDbContextFactory<ElibDbContext> dbFactory, ICryptoService crypto)
    {
        _dbFactory = dbFactory;
        _crypto    = crypto;
    }

    public async Task LogAsync(int? userId, string action, string? ip, CancellationToken ct = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var config = await db.LogActionConfigs
                .FirstOrDefaultAsync(c => c.ActionName == action, ct);

            if (config == null) return;

            var encryptedIp = !string.IsNullOrEmpty(ip) && _crypto.IsPlainText(ip)
                ? _crypto.Encrypt(ip) : ip;

            db.SystemLogs.Add(new SystemLog
            {
                UserId    = userId,
                Action    = action,
                IpAddress = encryptedIp,
                LevelId   = config.LevelId,
                Timestamp = DbTime.Now
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
