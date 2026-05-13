// Configuration options for the SendSay email delivery service including API key, URL, and draft IDs.
namespace Server.Services;

public class SendSayOptions
{
    public string ApiKey    { get; set; } = string.Empty;
    public string ApiUrl    { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public DraftOptions DraftsId { get; set; } = new();

    public class DraftOptions
    {
        public int RecoveryPasswordId { get; set; }
        public int RegistrationSuccessId { get; set; }
    }
}
