// Interface and record types defining the authentication service contract.
using Server.GraphQL.Types;

namespace Server.Services;

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Phone,
    int YearOfBirth);

public record AuthResult(string AccessToken, string RefreshToken, UserDto User);

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, string ipAddress, string deviceInfoJson, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(string email, string password, string ipAddress, string deviceInfoJson, CancellationToken ct = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> RequestPasswordResetAsync(string email, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct = default);
    Task<bool> ChangePasswordByEmailCodeAsync(int userId, string currentCode, string newPassword, CancellationToken ct = default);
    Task ResetUserPasswordByAdminAsync(int userId, string newPassword, CancellationToken ct = default);
}
