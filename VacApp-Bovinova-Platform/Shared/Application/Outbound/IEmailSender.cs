namespace VacApp_Bovinova_Platform.Shared.Application.Outbound;
public interface IEmailSender { Task SendAsync(string to, string subject, string htmlBody, string? idempotencyKey = null); }
