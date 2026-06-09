using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using ContactFormApi.Models;
using ContactFormApi.Data;
using ContactFormApi.Services;

namespace ContactFormApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly ILogger<ContactController> _logger;
    private readonly IValidator<ContactFormModel> _validator;
    private readonly AppDbContext _db;
    private readonly BlobService _blob;
    private readonly ServiceBusPublisher _bus;

    public ContactController(
        ILogger<ContactController> logger,
        IValidator<ContactFormModel> validator,
        AppDbContext db,
        BlobService blob,
        ServiceBusPublisher bus)
    {
        _logger    = logger;
        _validator = validator;
        _db        = db;
        _blob      = blob;
        _bus       = bus;
    }

    /// <summary>Submit the contact form with optional file attachment.</summary>
    [HttpPost("submit")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max request
    [ProducesResponseType(typeof(ContactFormResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromForm] ContactFormModel form)
    {
        // Validate form fields
        var result = await _validator.ValidateAsync(form);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        // Upload file to Blob Storage if provided
        string? attachmentUrl  = null;
        string? attachmentName = null;

        if (form.Attachment != null && form.Attachment.Length > 0)
        {
            try
            {
                attachmentUrl  = await _blob.UploadAsync(form.Attachment);
                attachmentName = form.Attachment.FileName;
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Save to database
        var submission = new ContactSubmission
        {
            FirstName      = form.FirstName,
            LastName       = form.LastName,
            Email          = form.Email,
            Phone          = form.Phone,
            Subject        = form.Subject,
            Message        = form.Message,
            ReferenceId    = Guid.NewGuid(),
            SubmittedAt    = DateTime.UtcNow,
            AttachmentUrl  = attachmentUrl,
            AttachmentName = attachmentName
        };

        _db.ContactSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Saved submission Ref:{ReferenceId} From:{Email} Attachment:{HasFile}",
            submission.ReferenceId, submission.Email, attachmentUrl != null);

        // Publish to Service Bus for async processing (email notifications etc.)
        await _bus.PublishSubmissionAsync(submission);

        return Ok(new ContactFormResponse
        {
            Success     = true,
            Message     = "Thank you! Your message has been received. We will get back to you shortly.",
            ReferenceId = submission.ReferenceId,
            SubmittedAt = submission.SubmittedAt
        });
    }

    /// <summary>Get all submissions — newest first.</summary>
    [HttpGet("list")]
    [ProducesResponseType(typeof(IEnumerable<ContactSubmission>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var submissions = await _db.ContactSubmissions
            .OrderByDescending(x => x.SubmittedAt)
            .ToListAsync();
        return Ok(submissions);
    }

    /// <summary>Health check.</summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
