using Yummy.Core.Services;

namespace Yummy.Tests.Infrastructure
{
    public class FakeEmailService : IEmailService
    {
        public List<(string To, string Subject)> SentEmails { get; } = new();

        public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            lock (SentEmails)
                SentEmails.Add((toEmail, subject));

            return Task.CompletedTask;
        }
    }
}
