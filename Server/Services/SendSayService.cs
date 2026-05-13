// HTTP client service that sends transactional emails via the SendSay API for registration and password reset.
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Server.Services;

public class SendSayService : IEmailService
{
    private readonly HttpClient _http;
    private readonly SendSayOptions _options;

    public SendSayService(HttpClient http, IOptions<SendSayOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task SendRegistrationSuccessAsync(string toEmail, string firstName)
    {
        var htmlBody = $"<p>Здравствуйте, {firstName}!</p>" +
                       $"<p>Вы успешно зарегистрировались в системе ELib.</p>" +
                       $"<p>Теперь вам доступен полный каталог электронной библиотеки.</p>" +
                       $"<p>С уважением, команда ELib.</p>";

        var payload = new Dictionary<string, object>
        {
            ["action"]     = "issue.send",
            ["apikey"]     = _options.ApiKey,
            ["sendwhen"]   = "now",
            ["group"]      = "personal",
            ["users.list"] = toEmail,
            ["letter"]     = new Dictionary<string, object>
            {
                ["subject"]    = "Добро пожаловать в ELib!",
                ["from.email"] = _options.FromEmail,
                ["from.name"]  = "ELib",
                ["message"]    = new Dictionary<string, object>
                {
                    ["html"] = htmlBody
                }
            }
        };

        await PostAndVerifyAsync(payload);
    }

    public async Task SendPasswordResetAsync(string toEmail, string firstName, string resetCode)
    {
        var htmlBody = $"<p>Здравствуйте, {firstName}!</p>" +
                       $"<p>Ваш код восстановления пароля: <strong style=\"font-size:24px;letter-spacing:6px\">{resetCode}</strong></p>" +
                       $"<p>Код действителен 15 минут.</p>" +
                       $"<p>С уважением, команда ELib.</p>";

        var payload = new Dictionary<string, object>
        {
            ["action"]     = "issue.send",
            ["apikey"]     = _options.ApiKey,
            ["sendwhen"]   = "now",
            ["group"]      = "personal",
            ["users.list"] = toEmail,
            ["letter"]     = new Dictionary<string, object>
            {
                ["subject"]    = "Код восстановления пароля",
                ["from.email"] = _options.FromEmail,
                ["from.name"]  = "ELib",
                ["message"]    = new Dictionary<string, object>
                {
                    ["html"] = htmlBody
                }
            }
        };

        await PostAndVerifyAsync(payload);
    }

    private async Task PostAndVerifyAsync(object payload)
    {
        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(_options.ApiUrl, content);

        var body = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        try
        {
            var node     = JsonNode.Parse(body);
            var errors   = node?["errors"]?.AsArray();
            var warnings = node?["warnings"]?.AsArray();

            if (errors != null && errors.Count > 0)
            {
                var first   = errors[0];
                var id      = first?["id"]?.GetValue<string>()      ?? "UNKNOWN";
                var explain = first?["explain"]?.GetValue<string>() ?? body;
                throw new InvalidOperationException($"SendSay error [{id}]: {explain}");
            }
        }
        catch (JsonException) { }
    }
}
