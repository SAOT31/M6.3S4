using System.IO;
using System.Net;
using System.Net.Mail;
using Firmeza.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Firmeza.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtp = _configuration.GetSection("SmtpSettings");
        var host = smtp["Host"];
        var portStr = smtp["Port"];
        var username = smtp["Username"];
        var password = smtp["Password"];
        var senderEmail = smtp["SenderEmail"] ?? username ?? string.Empty;
        var senderName = smtp["SenderName"] ?? "Firmeza";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || password == "your-app-password" || username.Contains("@example.com"))
        {
            _logger.LogWarning("SMTP no está configurado. Simulación de envío de correo a: {To}", to);
            return;
        }

        int port = int.TryParse(portStr, out var p) ? p : 587;
        bool enableSsl = !bool.TryParse(smtp["EnableSsl"], out var ssl) || ssl;

        try
        {
            using var client = new SmtpClient(host ?? string.Empty, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            var mail = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(to);

            await client.SendMailAsync(mail);
            _logger.LogInformation("Correo enviado a {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo a {To}", to);
            throw;
        }
    }

    public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachmentBytes, string attachmentName)
    {
        var smtp = _configuration.GetSection("SmtpSettings");
        var host = smtp["Host"];
        var portStr = smtp["Port"];
        var username = smtp["Username"];
        var password = smtp["Password"];
        var senderEmail = smtp["SenderEmail"] ?? username ?? string.Empty;
        var senderName = smtp["SenderName"] ?? "Firmeza";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || password == "your-app-password" || username.Contains("@example.com"))
        {
            _logger.LogWarning("SMTP no está configurado. Simulación de envío de correo con adjunto '{AttachmentName}' a: {To}", attachmentName, to);
            return;
        }

        int port = int.TryParse(portStr, out var p) ? p : 587;
        bool enableSsl = !bool.TryParse(smtp["EnableSsl"], out var ssl) || ssl;

        try
        {
            using var client = new SmtpClient(host ?? string.Empty, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            var mail = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(to);

            using var ms = new MemoryStream(attachmentBytes);
            var attachment = new Attachment(ms, attachmentName, "application/pdf");
            mail.Attachments.Add(attachment);

            await client.SendMailAsync(mail);
            _logger.LogInformation("Correo con adjunto '{AttachmentName}' enviado a {To}", attachmentName, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo con adjunto '{AttachmentName}' a {To}", attachmentName, to);
            throw;
        }
    }
}
