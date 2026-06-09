using Azure.Messaging.ServiceBus;
using ContactFormApi.Models;
using System.Text.Json;

namespace ContactFormApi.Services;

public class ServiceBusPublisher
{
    private readonly ServiceBusSender? _sender;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly bool _isConfigured;

    public ServiceBusPublisher(IConfiguration config, ILogger<ServiceBusPublisher> logger)
    {
        _logger = logger;
        var connStr = config["ServiceBus:ConnectionString"]
                   ?? config["ServiceBus__ConnectionString"];

        if (string.IsNullOrWhiteSpace(connStr))
        {
            _logger.LogWarning("ServiceBus connection string not configured. Messages will not be published.");
            _isConfigured = false;
            return;
        }

        try
        {
            var client = new ServiceBusClient(connStr);
            _sender = client.CreateSender("contact-submissions");
            _isConfigured = true;
            _logger.LogInformation("Service Bus configured successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Service Bus. Messages will not be published.");
            _isConfigured = false;
        }
    }

    public async Task PublishSubmissionAsync(ContactSubmission submission)
    {
        if (!_isConfigured || _sender is null)
        {
            _logger.LogWarning("Service Bus not configured — skipping message publish.");
            return;
        }

        var payload = new
        {
            submission.Id,
            submission.ReferenceId,
            submission.FirstName,
            submission.LastName,
            submission.Email,
            submission.Subject,
            submission.SubmittedAt,
            HasAttachment = submission.AttachmentUrl != null
        };

        var json = JsonSerializer.Serialize(payload);
        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            Subject = $"New contact form submission from {submission.Email}"
        };

        await _sender.SendMessageAsync(message);
        _logger.LogInformation("Published submission {ReferenceId} to Service Bus.", submission.ReferenceId);
    }
}
