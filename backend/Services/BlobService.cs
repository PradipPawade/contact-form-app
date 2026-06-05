using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ContactFormApi.Services;

public class BlobService
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<BlobService> _logger;

    // Allowed file types
    private static readonly HashSet<string> AllowedExtensions =
        [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt"];

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public BlobService(IConfiguration config, ILogger<BlobService> logger)
    {
        _logger = logger;
        var connStr = config["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

        var serviceClient = new BlobServiceClient(connStr);
        _container = serviceClient.GetBlobContainerClient("attachments");
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string?> UploadAsync(IFormFile file)
    {
        // Validate size
        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File size exceeds the 5 MB limit.");

        // Validate extension
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        // Create unique file name to prevent collisions
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var blobClient = _container.GetBlobClient(uniqueName);

        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = file.ContentType
        };

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders
        });

        _logger.LogInformation("Uploaded blob: {BlobName}", uniqueName);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string fileUrl)
    {
        var uri  = new Uri(fileUrl);
        var name = Path.GetFileName(uri.LocalPath);
        var blob = _container.GetBlobClient(name);
        await blob.DeleteIfExistsAsync();
    }
}
