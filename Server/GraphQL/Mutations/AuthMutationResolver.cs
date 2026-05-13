// GraphQL mutation resolver handling user registration, login, token refresh, logout, and password reset.
using HotChocolate;
using Microsoft.AspNetCore.Http;
using Server.GraphQL.Inputs;
using Server.GraphQL.Types;
using Server.Services;

namespace Server.GraphQL.Mutations;

[ExtendObjectType<Mutation>]
public class AuthMutationResolver
{
    [GraphQLName("register")]
    public async Task<AuthPayload?> Register(
        RegisterInput input,
        [Service] IAuthService authService,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var ip = http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authService.RegisterAsync(
            new RegisterRequest(input.Username, input.Email, input.Password,
                input.FirstName, input.LastName, input.MiddleName, input.Phone, input.YearOfBirth),
            ip, input.DeviceInfo ?? "{}", cancellationToken);

        return new AuthPayload { AccessToken = result.AccessToken, RefreshToken = result.RefreshToken, User = result.User };
    }

    [GraphQLName("login")]
    public async Task<AuthPayload?> Login(
        LoginInput input,
        [Service] IAuthService authService,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        try
        {
            var ip = http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await authService.LoginAsync(input.Email, input.Password, ip, input.DeviceInfo ?? "{}", cancellationToken);
            return new AuthPayload { AccessToken = result.AccessToken, RefreshToken = result.RefreshToken, User = result.User };
        }
        catch (GraphQLException) { throw; }
        catch (Exception ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Ошибка входа: " + ex.Message)
                    .SetCode("INTERNAL_ERROR")
                    .Build());
        }
    }

    [GraphQLName("refresh_token")]
    public async Task<AuthPayload?> RefreshToken(
        RefreshTokenInput input,
        [Service] IAuthService authService,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var ip = http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authService.RefreshTokenAsync(input.RefreshToken, ip, cancellationToken);
        return new AuthPayload { AccessToken = result.AccessToken, RefreshToken = result.RefreshToken, User = result.User };
    }

    [GraphQLName("logout")]
    public async Task<bool> Logout(
        RefreshTokenInput input,
        [Service] IAuthService authService,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(input.RefreshToken, cancellationToken);
        return true;
    }

    [GraphQLName("request_password_reset")]
    public async Task<bool> RequestPasswordReset(
        RequestPasswordResetInput input,
        [Service] IAuthService authService,
        CancellationToken cancellationToken)
    {
        try
        {
            var sent = await authService.RequestPasswordResetAsync(input.Email, cancellationToken);
            if (!sent)
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("Пользователь с таким email не найден")
                        .SetCode("NOT_FOUND")
                        .Build());
            return true;
        }
        catch (GraphQLException) { throw; }
        catch (Exception)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Не удалось отправить письмо. Попробуйте позже.")
                    .SetCode("EMAIL_SEND_FAILED")
                    .Build());
        }
    }

    [GraphQLName("reset_password")]
    public async Task<bool> ResetPassword(
        ResetPasswordInput input,
        [Service] IAuthService authService,
        CancellationToken cancellationToken)
    {
        return await authService.ResetPasswordAsync(input.Email, input.Code, input.NewPassword, cancellationToken);
    }
}
