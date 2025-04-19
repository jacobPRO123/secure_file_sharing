using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Security;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SecureFileSharingApp.Services
{
    public class MailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailService> _logger;

        // Constructor injection for IConfiguration and ILogger
        public MailService(IConfiguration config, ILogger<MailService> logger)
        {
            _configuration = config;
            
            _logger = logger;
        }

        public async Task SendEmailAsync(string subject, string message)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("your-email@example.com"));
            email.To.Add(MailboxAddress.Parse("recipient@example.com"));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Text) { Text = message };

            using var smtp = new SmtpClient();

            try
            {
                // Fetch SMTP settings from configuration
                string smtpHost = _configuration["EmailSettings:SmtpHost"];
                string smtpPortStr = _configuration["EmailSettings:SmtpPort"];  // Port as string
                string smtpUser = _configuration["EmailSettings:SmtpUser"];
                string smtpPass = _configuration["EmailSettings:SmtpPass"];

                // Check if any of the settings are missing
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortStr) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                {
                    throw new InvalidOperationException("Missing SMTP configuration values.");
                }

                // Validate if the SMTP port is a valid integer and within the typical port range for SMTP (25, 465, 587)
                if (!int.TryParse(smtpPortStr, out int smtpPort) || smtpPort < 1 || smtpPort > 65535)
                {
                    throw new InvalidOperationException("Invalid SMTP port number.");
                }

                // Additional check for commonly used SMTP ports (optional)
                var validPorts = new[] { 25, 465, 587 };
                if (!validPorts.Contains(smtpPort))
                {
                    _logger.LogWarning("SMTP port is uncommon: {Port}. Ensure the port is correct.", smtpPort);
                }

                // Connect to the SMTP server with the validated port
                await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Failed to connect to SMTP server.");
                throw new InvalidOperationException("Unable to connect to the email server.", ex);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid port number format.");
                throw new InvalidOperationException("Invalid SMTP port format.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed.");
                throw new InvalidOperationException("Failed to send email.", ex);
            }
        }

    }
}

