using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ContactFormFunctions.Functions;

public class ContactSubmissionProcessor
{
    private readonly ILogger<ContactSubmissionProcessor> _logger;

    public ContactSubmissionProcessor(ILogger<ContactSubmissionProcessor> logger)
    {
        _logger = logger;
    }

    [Function("ContactSubmissionProcessor")]
    public void Run(
        [ServiceBusTrigger("contact-submissions", Connection = "ServiceBusConnection")]
        string messageBody)
    {
        _logger.LogInformation("Processing contact submission message.");

        try
        {
            using var doc = JsonDocument.Parse(messageBody);
            var root = doc.RootElement;

            var referenceId = root.GetProperty("referenceId").GetGuid();
            var email       = root.GetProperty("email").GetString();
            var firstName   = root.GetProperty("firstName").GetString();
            var subject     = root.GetProperty("subject").GetString();
            var submittedAt = root.GetProperty("submittedAt").GetDateTime();
            var hasAttachment = root.GetProperty("hasAttachment").GetBoolean();

            _logger.LogInformation(
                "New submission received — Ref: {ReferenceId} | From: {Email} ({FirstName}) | Subject: {Subject} | Submitted: {SubmittedAt} | HasAttachment: {HasAttachment}",
                referenceId, email, firstName, subject, submittedAt, hasAttachment);

            // TODO: extend this to send a confirmation email, notify via Teams/Slack, etc.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message: {Body}", messageBody);
            throw; // rethrow so Service Bus retries the message
        }
    }
}
