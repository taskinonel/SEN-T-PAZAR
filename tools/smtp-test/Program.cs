using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? Environment.GetEnvironmentVariable("Smtp__Host") ?? "";
        var portText = Environment.GetEnvironmentVariable("SMTP_PORT") ?? Environment.GetEnvironmentVariable("Smtp__Port") ?? "587";
        var user = Environment.GetEnvironmentVariable("SMTP_USER") ?? Environment.GetEnvironmentVariable("Smtp__User") ?? "";
        var pass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? Environment.GetEnvironmentVariable("Smtp__Pass") ?? "";
        var from = Environment.GetEnvironmentVariable("SMTP_FROM") ?? Environment.GetEnvironmentVariable("Smtp__From") ?? user;
        var to = Environment.GetEnvironmentVariable("SMTP_TO") ?? (args.Length > 0 ? args[0] : "");
        var subject = Environment.GetEnvironmentVariable("SMTP_SUBJECT") ?? (args.Length > 1 ? args[1] : "Test email from sentpazar");
        var body = Environment.GetEnvironmentVariable("SMTP_BODY") ?? (args.Length > 2 ? args[2] : "This is a test email from sentpazar SMTP test tool.");

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            Console.Error.WriteLine("Missing required SMTP parameters. Set env vars SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS, SMTP_FROM and SMTP_TO (or pass recipient as first arg).");
            return 2;
        }

        if (!int.TryParse(portText, out var port)) port = 587;

        try
        {
            using var client = new SmtpClient(host, port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            using var mail = new MailMessage(from, to, subject, body) { IsBodyHtml = false };
            await client.SendMailAsync(mail);
            Console.WriteLine("OK: Email sent to " + to);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex);
            return 1;
        }
    }
}
