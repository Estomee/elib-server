// Configuration options for JWT token issuance including secret key, issuer, audience, and expiry settings.
namespace Server.Services;

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpireMinutes { get; set; } = 60;
    public int RefreshTokenExpireDays { get; set; } = 30;
}
