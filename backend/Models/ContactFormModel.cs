namespace ContactFormApi.Models;

public class ContactFormModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public string Phone     { get; set; } = string.Empty;
    public string Subject   { get; set; } = string.Empty;
    public string Message        { get; set; } = string.Empty;
    public string? AttachmentUrl  { get; set; }  // blob URL after direct upload
    public string? AttachmentName { get; set; }  // original filename
}

public class ContactFormResponse
{
    public bool   Success   { get; set; }
    public string Message   { get; set; } = string.Empty;
    public Guid   ReferenceId { get; set; }
    public DateTime SubmittedAt { get; set; }
}
