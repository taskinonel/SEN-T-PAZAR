using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SEN_T_PAZAR.Services;

public class EmailSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _from;

    public EmailSender(string smtpHost, int smtpPort, string smtpUser, string smtpPass, string from)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
        _smtpUser = smtpUser;
        _smtpPass = smtpPass;
        _from = from;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        // Special dev sink: if smtpHost is set to "__DEV_SINK__", just write emails to disk for inspection
        if (string.Equals(_smtpHost, "__DEV_SINK__", StringComparison.Ordinal))
        {
            try
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "test-emails");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, $"email_{DateTime.UtcNow:yyyyMMddHHmmssfff}.html");
                File.WriteAllText(file, $"To: {to}\nSubject: {subject}\n\n{body}", Encoding.UTF8);
            }
            catch { }

            return;
        }

        if (string.IsNullOrWhiteSpace(_smtpHost) || _smtpPort <= 0 || string.IsNullOrWhiteSpace(_from))
        {
            throw new InvalidOperationException("SMTP host, port or sender address is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_smtpUser) || string.IsNullOrWhiteSpace(_smtpPass))
        {
            throw new InvalidOperationException("SMTP credentials are missing.");
        }

        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_smtpUser, _smtpPass),
            EnableSsl = true
        };
        using var mail = new MailMessage(_from, to, subject, body) { IsBodyHtml = true };
        await client.SendMailAsync(mail);
    }
}
