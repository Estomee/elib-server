// GraphQL mutation resolver for updating user profile and resetting passwords.
using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.GraphQL.Inputs;
using Server.GraphQL.Types;
using Server.Models;
using Server.Services;

namespace Server.GraphQL.Mutations;

[ExtendObjectType<Mutation>]
public class UserMutationResolver
{
    [GraphQLName("update_user")]
    [Authorize]
    public async Task<UserDto> UpdateUser(
        UpdateUserInput input,
        [Service] ElibDbContext db,
        [Service] ILogService log,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(http);
        var ip     = http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null)
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Пользователь не найден.").SetCode("USER_NOT_FOUND").Build());

        if (input.FirstName != null)   user.FirstName   = input.FirstName;
        if (input.LastName != null)    user.LastName    = input.LastName;
        if (input.MiddleName != null)  user.MiddleName  = input.MiddleName;
        if (input.Phone != null)       user.Phone       = input.Phone;
        if (input.YearOfBirth != null) user.YearOfBirth = input.YearOfBirth.Value;

        await db.SaveChangesAsync(cancellationToken);

        await log.LogAsync(userId, "update_user", ip, cancellationToken);

        return AuthService.MapUser(user);
    }

    [GraphQLName("change_password_by_email_code")]
    [Authorize]
    public async Task<bool> ChangePasswordByEmailCode(
        ChangePasswordByEmailCodeInput input,
        [Service] IAuthService authService,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(http);
        return await authService.ChangePasswordByEmailCodeAsync(userId, input.Code, input.NewPassword, cancellationToken);
    }

    [GraphQLName("reset_user_password")]
    [Authorize]
    public async Task<bool> ResetUserPassword(
        ResetUserPasswordInput input,
        [Service] IAuthService authService,
        [Service] ILogService log,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var adminId = GetUserId(http);
        var ip      = http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        await authService.ResetUserPasswordByAdminAsync(input.UserId, input.NewPassword, cancellationToken);

        if (adminId.HasValue)
            await log.LogAsync(adminId, "reset_user_password", ip, cancellationToken);

        return true;
    }

    private static int RequireUserId(IHttpContextAccessor http)
    {
        var claim = http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim == null || !int.TryParse(claim, out var id))
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Требуется аутентификация.").SetCode("UNAUTHORIZED").Build());
        return id;
    }

    private static int? GetUserId(IHttpContextAccessor http)
    {
        var claim = http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
