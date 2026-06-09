using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text.Json;

namespace ContactFormFunctions.Functions;

public class ContactSubmissionProcessor
{
    private readonly ILogger<ContactSubmissionProcessor> _logger;
    private readonly IConfiguration _config;

    public ContactSubmissionProcessor(ILogger<ContactSubmissionProcessor> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    [Function("ContactSubmissionProcessor")]
    public async Task Run(
        [ServiceBusTrigger("contact-submissions", Connection = "ServiceBusConnection")]
        string messageBody)
    {
        _logger.LogInformation("Processing contact submission message.");

        try
        {
            using var doc = JsonDocument.Parse(messageBody);
            var root = doc.RootElement;

            var referenceId   = root.GetProperty("referenceId").GetGuid();
            var email         = root.GetProperty("email").GetString() ?? "";
            var firstName     = root.GetProperty("firstName").GetString() ?? "";
            var lastName      = root.GetProperty("lastName").GetString() ?? "";
            var subject       = root.GetProperty("subject").GetString() ?? "";
            var submittedAt   = root.GetProperty("submittedAt").GetDateTime();
            var hasAttachment = root.GetProperty("hasAttachment").GetBoolean();

            _logger.LogInformation(
                "Submission received — Ref: {ReferenceId} | From: {Email} | Subject: {Subject}",
                referenceId, email, subject);

            await SendNotificationEmailAsync(referenceId, email, firstName, lastName, subject, submittedAt, hasAttachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message: {Body}", messageBody);
            throw; // rethrow so Service Bus retries
        }
    }

    private async Task SendNotificationEmailAsync(
        Guid referenceId, string senderEmail, string firstName, string lastName,
        string subject, DateTime submittedAt, bool hasAttachment)
    {
        var apiKey = _config["SendGrid__ApiKey"] ?? _config["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("SendGrid API key not configured — skipping email.");
            return;
        }

        var client   = new SendGridClient(apiKey);
        var from     = new EmailAddress("pradippawade123@gmail.com", "Contact Form");
        var to       = new EmailAddress("pradippawade123@gmail.com", "Pradip Pawade");
        var emailSubject = $"New Contact Form Submission — {subject}";

        var htmlBody = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
  <h2 style='color: #2d6cdf;'>New Contact Form Submission</h2>
  <table style='width:100%; border-collapse: collapse;'>
    <tr><td style='padding:8px; font-weight:bold; color:#555;'>Reference ID</td>
        <td style='padding:8px;'>{referenceId}</td></tr>
    <tr style='background:#f9f9f9;'>
        <td style='padding:8px; font-weight:bold; color:#555;'>Name</td>
        <td style='padding:8px;'>{firstName} {lastName}</td></tr>
    <tr><td style='padding:8px; font-weight:bold; color:#555;'>Email</td>
        <td style='padding:8px;'><a href='mailto:{senderEmail}'>{senderEmail}</a></td></tr>
    <tr style='background:#f9f9f9;'>
        <td style='padding:8px; font-weight:bold; color:#555;'>Subject</td>
        <td style='padding:8px;'>{subject}</td></tr>
    <tr><td style='padding:8px; font-weight:bold; color:#555;'>Submitted At</td>
        <td style='padding:8px;'>{submittedAt:dd MMM yyyy, hh:mm tt} UTC</td></tr>
    <tr style='background:#f9f9f9;'>
        <td style='padding:8px; font-weight:bold; color:#555;'>Attachment</td>
        <td style='padding:8px;'>{(hasAttachment ? "Yes" : "No")}</td></tr>
  </table>
  <p style='margin-top:20px; color:#888; font-size:12px;'>
    This is an automated notification from your Contact Form app.
  </p>
</div>";

        var msg = MailHelper.CreateSingleEmail(from, to, emailSubject, plainTextContent: null, htmlContent: htmlBody);
        var response = await client.SendEmailAsync(msg);

        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            _logger.LogInformation("Notification email sent successfully. Status: {Status}", response.StatusCode);
        else
            _logger.LogError("SendGrid returned error status: {Status}", response.StatusCode);
    }
}
