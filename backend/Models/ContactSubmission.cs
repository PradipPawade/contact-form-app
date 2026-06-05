namespace ContactFormApi.Models;

/// <summary>Entity stored in the database for every form submission.</summary>
public class ContactSubmission
{
    public int      Id          { get; set; }
    public string   FirstName   { get; set; } = string.Empty;
    public string   LastName    { get; set; } = string.Empty;
    public string   Email       { get; set; } = string.Empty;
    public string   Phone       { get; set; } = string.Empty;
    public string   Subject     { get; set; } = string.Empty;
    public string   Message     { get; set; } = string.Empty;
    public Guid     ReferenceId   { get; set; } = Guid.NewGuid();
    public DateTime SubmittedAt   { get; set; } = DateTime.UtcNow;
    public string?  AttachmentUrl { get; set; }          // Blob Storage URL
    public string?  AttachmentName { get; set; }         // Original file name
}
