// GraphQL input types for authentication operations: login, register, token refresh, and password reset.
using HotChocolate;

namespace Server.GraphQL.Inputs;

[GraphQLName("LoginInput")]
public class LoginInput
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
}

[GraphQLName("RegisterInput")]
public class RegisterInput
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public int YearOfBirth { get; set; }
    public string? DeviceInfo { get; set; }
}

[GraphQLName("RefreshTokenInput")]
public class RefreshTokenInput
{
    public string RefreshToken { get; set; } = string.Empty;
}

[GraphQLName("RequestPasswordResetInput")]
public class RequestPasswordResetInput
{
    public string Email { get; set; } = string.Empty;
}

[GraphQLName("ResetPasswordInput")]
public class ResetPasswordInput
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
