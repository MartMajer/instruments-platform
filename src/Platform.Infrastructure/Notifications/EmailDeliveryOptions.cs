using Platform.Application.Features.Notifications;
using System.Net.Mail;

namespace Platform.Infrastructure.Notifications;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "EmailDelivery";

    public string Provider { get; set; } = EmailDeliveryProviderNames.LocalDev;

    public string? FromAddress { get; set; }

    public SmtpEmailDeliveryOptions Smtp { get; set; } = new();

    public void EnsureValidProviderConfiguration()
    {
        if (string.Equals(Provider, EmailDeliveryProviderNames.LocalDev, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.Equals(Provider, EmailDeliveryProviderNames.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"EmailDelivery:Provider must be '{EmailDeliveryProviderNames.LocalDev}' or '{EmailDeliveryProviderNames.Smtp}'.");
        }

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Smtp.Host))
        {
            missing.Add("EmailDelivery:Smtp:Host");
        }
        else if (!IsSafeSmtpHost(Smtp.Host))
        {
            missing.Add("EmailDelivery:Smtp:Host");
        }

        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            missing.Add("EmailDelivery:FromAddress");
        }
        else
        {
            try
            {
                _ = new MailAddress(FromAddress);
            }
            catch (FormatException)
            {
                missing.Add("EmailDelivery:FromAddress");
            }
        }

        if (Smtp.Port is < 1 or > 65535)
        {
            missing.Add("EmailDelivery:Smtp:Port");
        }

        var hasUserName = !string.IsNullOrWhiteSpace(Smtp.UserName);
        var hasPassword = !string.IsNullOrWhiteSpace(Smtp.Password);
        if (hasUserName != hasPassword)
        {
            missing.Add("EmailDelivery:Smtp:Credentials");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"SMTP email delivery is enabled but required configuration is missing: {string.Join(", ", missing)}.");
        }
    }

    private static bool IsSafeSmtpHost(string host)
    {
        var normalized = host.Trim();
        if (normalized.Length == 0 ||
            normalized.Length != host.Length ||
            normalized.Length > 253)
        {
            return false;
        }

        foreach (var character in normalized)
        {
            if (char.IsControl(character) ||
                char.IsWhiteSpace(character))
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class SmtpEmailDeliveryOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 25;

    public bool EnableSsl { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }
}
