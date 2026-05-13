// Service implementing all authentication logic: registration, login, token refresh, logout, and password resets.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.GraphQL.Types;
using Server.Models;

namespace Server.Services;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<ElibDbContext> _dbFactory;
    private readonly ISecurityService _security;
    private readonly IEmailService _email;
    private readonly JwtOptions _jwt;
    private readonly ILogService _log;

    public AuthService(
        IDbContextFactory<ElibDbContext> dbFactory,
        ISecurityService security,
        IEmailService email,
        IOptions<JwtOptions> jwtOptions,
        ILogService log)
    {
        _dbFactory = dbFactory;
        _security  = security;
        _email     = email;
        _jwt       = jwtOptions.Value;
        _log       = log;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string ipAddress, string deviceInfoJson, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var exists = await db.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username, ct);
        if (exists)
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Пользователь с таким email или именем пользователя уже зарегистрирован.")
                    .SetCode("USER_ALREADY_EXISTS")
                    .Build());

        var now = DbTime.Now;
        var user = new User
        {
            Username          = request.Username,
            Email             = request.Email,
            PasswordHash      = _security.HashPassword(request.Password),
            FirstName         = request.FirstName,
            LastName          = request.LastName,
            MiddleName        = request.MiddleName,
            Phone             = request.Phone,
            YearOfBirth       = request.YearOfBirth,
            Employee          = false,
            RegistrationDate  = now,
            LastLogin         = now,
            LastPasswordChange = now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var session = await CreateSessionAsync(db, user, ipAddress, deviceInfoJson, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        try { await _email.SendRegistrationSuccessAsync(user.Email, user.FirstName); }
        catch (Exception ex) { _ = ex; }

        await _log.LogAsync(user.UserId, "register", ipAddress, ct);

        return new AuthResult(session.AccessToken, session.RefreshToken, MapUser(user));
    }

    public async Task<AuthResult> LoginAsync(string email, string password, string ipAddress, string deviceInfoJson, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null || !_security.VerifyPassword(password, user.PasswordHash))
        {
            await _log.LogAsync(user?.UserId, "login_failed", ipAddress, ct);
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Неверный email или пароль.")
                    .SetCode("INVALID_CREDENTIALS")
                    .Build());
        }

        user.LastLogin = DbTime.Now;

        var session = await CreateSessionAsync(db, user, ipAddress, deviceInfoJson, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await _log.LogAsync(user.UserId, "login", ipAddress, ct);

        return new AuthResult(session.AccessToken, session.RefreshToken, MapUser(user));
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var session = await db.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.ExpiresAt > DateTime.UtcNow, ct);

        if (session == null)
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Сессия недействительна или истекла.")
                    .SetCode("INVALID_REFRESH_TOKEN")
                    .Build());

        db.UserSessions.Remove(session);
        var newSession = await CreateSessionAsync(db, session.User, ipAddress, session.DeviceInfo, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new AuthResult(newSession.AccessToken, newSession.RefreshToken, MapUser(session.User));
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var session = await db.UserSessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, ct);
        if (session == null) return;

        db.UserSessions.Remove(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null) return false;

        var now = DbTime.Now;
        var recentCode = await db.PasswordResetCodes
            .Where(c => c.UserId == user.UserId && c.ExpiresAt > now && c.Used != true)
            .OrderByDescending(c => c.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        if (recentCode != null && (recentCode.ExpiresAt - TimeSpan.FromMinutes(15)) > now.AddSeconds(-60))
            return true;

        var code = _security.GenerateSecureCode(6);
        db.PasswordResetCodes.Add(new PasswordResetCode
        {
            UserId    = user.UserId,
            Code      = code,
            ExpiresAt = now.AddMinutes(15),
            Used      = false
        });
        await db.SaveChangesAsync(ct);

        await _email.SendPasswordResetAsync(user.Email, user.FirstName, code);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null) return false;

        var now = DbTime.Now;
        var resetCode = await db.PasswordResetCodes.FirstOrDefaultAsync(c =>
            c.UserId == user.UserId &&
            c.Code == code &&
            c.ExpiresAt > now &&
            c.Used != true, ct);

        if (resetCode == null) return false;

        user.PasswordHash      = _security.HashPassword(newPassword);
        user.LastPasswordChange = now;
        resetCode.Used         = true;

        var sessions = await db.UserSessions.Where(s => s.UserId == user.UserId).ToListAsync(ct);
        db.UserSessions.RemoveRange(sessions);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await _log.LogAsync(user.UserId, "reset_password", null, ct);
        return true;
    }

    public async Task<bool> ChangePasswordByEmailCodeAsync(int userId, string currentCode, string newPassword, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null) return false;

        var now = DbTime.Now;
        var resetCode = await db.PasswordResetCodes.FirstOrDefaultAsync(c =>
            c.UserId == userId &&
            c.Code == currentCode &&
            c.ExpiresAt > now &&
            c.Used != true, ct);

        if (resetCode == null) return false;

        user.PasswordHash      = _security.HashPassword(newPassword);
        user.LastPasswordChange = now;
        resetCode.Used         = true;

        var sessions = await db.UserSessions.Where(s => s.UserId == userId).ToListAsync(ct);
        db.UserSessions.RemoveRange(sessions);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task ResetUserPasswordByAdminAsync(int userId, string newPassword, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Пользователь не найден.")
                    .SetCode("USER_NOT_FOUND")
                    .Build());

        var now = DbTime.Now;
        user.PasswordHash      = _security.HashPassword(newPassword);
        user.LastPasswordChange = now;

        var sessions = await db.UserSessions.Where(s => s.UserId == userId).ToListAsync(ct);
        db.UserSessions.RemoveRange(sessions);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task<(string AccessToken, string RefreshToken)> CreateSessionAsync(
        ElibDbContext db,
        User user,
        string ipAddress,
        string deviceInfoJson,
        CancellationToken ct)
    {
        var accessToken  = GenerateAccessToken(user);
        var refreshToken = _security.GenerateRefreshToken();

        db.UserSessions.Add(new UserSession
        {
            UserId       = user.UserId,
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            CreatedAt    = DbTime.Now,
            ExpiresAt    = DbTime.Now.AddDays(_jwt.RefreshTokenExpireDays),
            DeviceInfo   = deviceInfoJson,
            IpAddress    = ipAddress
        });

        return (accessToken, refreshToken);
    }

    private string GenerateAccessToken(User user)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("is_employee", user.Employee.ToString().ToLower())
        };

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    internal static UserDto MapUser(User user) => new()
    {
        UserId             = user.UserId.ToString(),
        Username           = user.Username,
        Email              = user.Email,
        FirstName          = user.FirstName,
        LastName           = user.LastName,
        MiddleName         = user.MiddleName,
        Phone              = user.Phone,
        YearOfBirth        = user.YearOfBirth,
        Employee           = user.Employee,
        RegistrationDate   = user.RegistrationDate,
        LastLogin          = user.LastLogin,
        LastPasswordChange = user.LastPasswordChange
    };
}
