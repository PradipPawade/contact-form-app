using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ContactFormApi.Models;

namespace ContactFormApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly ILogger<ContactController> _logger;
    private readonly IValidator<ContactFormModel> _validator;

    public ContactController(ILogger<ContactController> logger, IValidator<ContactFormModel> validator)
    {
        _logger    = logger;
        _validator = validator;
    }

    /// <summary>
    /// Submits the contact form.
    /// </summary>
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

        var referenceId = Guid.NewGuid();
        _logger.LogInformation(
            "Contact form submitted — Ref: {ReferenceId}, From: {Email}, Subject: {Subject}",
            referenceId, form.Email, form.Subject);

        // TODO: persist to DB or send email here
        await Task.CompletedTask;

        return Ok(new ContactFormResponse
        {
            Success     = true,
            Message     = "Thank you! Your message has been received. We will get back to you shortly.",
            ReferenceId = referenceId,
            SubmittedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
