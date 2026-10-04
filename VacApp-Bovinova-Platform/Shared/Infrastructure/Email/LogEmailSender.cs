using VacApp_Bovinova_Platform.Shared.Application.Outbound;
namespace VacApp_Bovinova_Platform.Shared.Infrastructure.Email;
public class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender {
    public Task SendAsync(string to, string subject, string htmlBody, string? idempotencyKey = null) {
        logger.LogInformation("Email to {To}: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
