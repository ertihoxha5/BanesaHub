using System.Net;
using System.Net.Mail;

namespace BanesaHub.API.Auth;

public interface IPasswordResetDelivery
{
    bool IsConfigured { get; }
    Task SendAsync(string recipient, string token, CancellationToken cancellationToken);
}

internal sealed class SmtpPasswordResetDelivery(IConfiguration configuration) : IPasswordResetDelivery
{
    private IConfigurationSection Settings => configuration.GetSection("PasswordReset:Smtp");

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Settings["Host"])
        && MailAddress.TryCreate(Settings["From"], out _);

    public async Task SendAsync(string recipient, string token, CancellationToken cancellationToken)
    {
        var settings = Settings;
        using var client = new SmtpClient(settings["Host"]!, settings.GetValue("Port", 587))
        {
            EnableSsl = true
        };
        if (settings["Username"] is { Length: > 0 } username)
        {
            client.Credentials = new NetworkCredential(username, settings["Password"]);
        }

        using var message = new MailMessage(settings["From"]!, recipient,
            "Rivendosja e fjalëkalimit",
            $"Kodi për rivendosjen e fjalëkalimit:\n\n{token}\n\nKodi skadon pas 30 minutash.");
        await client.SendMailAsync(message, cancellationToken);
    }
}
