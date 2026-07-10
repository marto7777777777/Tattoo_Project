using System.Net;
using System.Net.Mail;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class EmailService(IConfiguration configuration) : IEmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var host = configuration["EmailSettings:SmtpHost"];
            var port = int.Parse(configuration["EmailSettings:SmtpPort"] ?? "587");
            var username = configuration["EmailSettings:SmtpUsername"];
            var password = configuration["EmailSettings:SmtpPassword"];
            var senderEmail = configuration["EmailSettings:SenderEmail"];
            var senderName = configuration["EmailSettings:SenderName"] ?? "InkRoute";

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException(
                    "Email settings are missing. Please configure EmailSettings in appsettings.json or user secrets.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}
