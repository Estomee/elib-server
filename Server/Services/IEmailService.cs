// Interface defining email service operations for password reset and registration confirmation.
namespace Server.Services;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string firstName, string resetCode);
    Task SendRegistrationSuccessAsync(string toEmail, string firstName);
}
