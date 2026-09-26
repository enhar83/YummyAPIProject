using Yummy.Core.Services;

namespace Yummy.Tests.Infrastructure
{
    public class FakeEmailService : IEmailService
    {
        public List<(string To, string Subject)> SentEmails { get; } = new();

        // true yapılırsa SMTP hatası taklit edilir.
        public bool ShouldFail { get; set; }

        public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
                throw new InvalidOperationException("SMTP sunucusuna bağlanılamadı.");

            lock (SentEmails)
                SentEmails.Add((toEmail, subject));

            return Task.CompletedTask;
        }
    }
}
