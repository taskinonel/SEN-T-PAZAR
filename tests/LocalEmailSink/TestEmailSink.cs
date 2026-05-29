using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SEN_T_PAZAR.Tests.LocalEmailSink
{
    public static class TestEmailSink
    {
        public static readonly ConcurrentBag<EmailRecord> Emails = new();

        public static void Add(string to, string subject, string body)
        {
            Emails.Add(new EmailRecord { To = to, Subject = subject, Body = body });
            try
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "test-emails");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, $"email_{System.DateTime.UtcNow:yyyyMMddHHmmssfff}.html");
                File.WriteAllText(file, $"To: {to}\nSubject: {subject}\n\n{body}", Encoding.UTF8);
            }
            catch { }
        }

        public sealed class EmailRecord
        {
            public string To { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
        }
    }
}
