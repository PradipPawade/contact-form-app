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

    /// <summary>Get a short-lived SAS URL for direct browser-to-blob upload.</summary>
    [HttpGet("upload-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetUploadUrl([FromQuery] string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return BadRequest(new { error = "filename is required." });

        try
        {
            var result = _blob.GenerateSasUploadUrl(filename);
            if (result is null)
                return StatusCode(503, new { error = "Blob Storage is not configured." });

            return Ok(new { uploadUrl = result.UploadUrl, blobUrl = result.BlobUrl });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Submit the contact form. File should be uploaded directly to blob first via /upload-url.</summary>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(ContactFormResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] ContactFormModel form)
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
            AttachmentUrl  = form.AttachmentUrl,
            AttachmentName = form.AttachmentName
        };

        _db.ContactSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Saved submission Ref:{ReferenceId} From:{Email} Attachment:{HasFile}",
            submission.ReferenceId, submission.Email, submission.AttachmentUrl != null);

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
