using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using ContactFormApi.Models;
using ContactFormApi.Data;

namespace ContactFormApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly ILogger<ContactController> _logger;
    private readonly IValidator<ContactFormModel> _validator;
    private readonly AppDbContext _db;

    public ContactController(
        ILogger<ContactController> logger,
        IValidator<ContactFormModel> validator,
        AppDbContext db)
    {
        _logger    = logger;
        _validator = validator;
        _db        = db;
    }

    /// <summary>Submit the contact form — saves to database.</summary>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(ContactFormResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] ContactFormModel form)
    {
        var result = await _validator.ValidateAsync(form);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var submission = new ContactSubmission
        {
            FirstName   = form.FirstName,
            LastName    = form.LastName,
            Email       = form.Email,
            Phone       = form.Phone,
            Subject     = form.Subject,
            Message     = form.Message,
            ReferenceId = Guid.NewGuid(),
            SubmittedAt = DateTime.UtcNow
        };

        _db.ContactSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Saved submission Ref:{ReferenceId} From:{Email}",
            submission.ReferenceId, submission.Email);

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
