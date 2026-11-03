using Microsoft.Extensions.Logging;

namespace DonguselIz.Web.Services
{
    public class SimulatedEmailSender : IEmailSender
    {
        private readonly ILogger<SimulatedEmailSender> _logger;

        public SimulatedEmailSender(ILogger<SimulatedEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            _logger.LogInformation("================== E-POSTA BİLDİRİM SİMÜLASYONU ==================");
            _logger.LogInformation("Alıcı: {ToEmail}", toEmail);
            _logger.LogInformation("Konu: {Subject}", subject);
            _logger.LogInformation("Mesaj: {Message}", htmlMessage);
            _logger.LogInformation("===================================================================");
            return Task.CompletedTask;
        }
    }
}
