namespace DogukanSite.Services
{
    // Services/IEmailSender.cs (veya uygun bir klasörde)
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }

    // Services/EmailSender.cs (Geçici veya gerçek implementasyon)
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        public EmailSender(ILogger<EmailSender> logger) { _logger = logger; }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            // Buraya gerçek e-posta gönderme kodunuzu ekleyin (SMTP, SendGrid vb.)
            _logger.LogWarning($"E-POSTA GÖNDERİLMEDİ (gerçek implementasyon gerekli): Kime: {email}, Konu: {subject}, Mesaj: {message}");
            return Task.CompletedTask;
        }
    }
}
