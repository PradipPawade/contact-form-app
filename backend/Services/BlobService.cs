using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace ContactFormApi.Services;

public record SasUploadResult(string UploadUrl, string BlobUrl);

public class BlobService
{
    private readonly BlobServiceClient? _serviceClient;
    private readonly BlobContainerClient? _container;
    private readonly ILogger<BlobService> _logger;
    private readonly bool _isConfigured;

    private static readonly HashSet<string> AllowedExtensions =
        [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt"];

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public BlobService(IConfiguration config, ILogger<BlobService> logger)
    {
        _logger = logger;
        // Azure App Service maps AzureStorage__ConnectionString → AzureStorage:ConnectionString automatically,
        // but fall back to the double-underscore key in case it doesn't.
        var connStr = config["AzureStorage:ConnectionString"]
                   ?? config["AzureStorage__ConnectionString"];

        if (string.IsNullOrWhiteSpace(connStr))
        {
            _logger.LogWarning("AzureStorage connection string not configured. File uploads are disabled.");
            _isConfigured = false;
            return;
        }

        try
        {
            _serviceClient = new BlobServiceClient(connStr);
            _container = _serviceClient.GetBlobContainerClient("attachments");
            _container.CreateIfNotExists(PublicAccessType.Blob);
            _isConfigured = true;
            _logger.LogInformation("Blob Storage configured successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Blob Storage. File uploads are disabled.");
            _isConfigured = false;
        }
    }

    public async Task<string?> UploadAsync(IFormFile file)
    {
        if (!_isConfigured || _container is null)
        {
            _logger.LogWarning("Blob Storage not configured — skipping file upload.");
            return null;
        }

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File size exceeds the 5 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var blobClient = _container.GetBlobClient(uniqueName);

        var blobHttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType };

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders
        });

        _logger.LogInformation("Uploaded blob: {BlobName}", uniqueName);
        return blobClient.Uri.ToString();
    }

    public SasUploadResult? GenerateSasUploadUrl(string originalFilename)
    {
        if (!_isConfigured || _container is null)
        {
            _logger.LogWarning("Blob Storage not configured — cannot generate SAS URL.");
            return null;
        }

        var ext = Path.GetExtension(originalFilename).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        var blobName = $"{Guid.NewGuid()}{ext}";
        var blobClient = _container.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _container.Name,
            BlobName          = blobName,
            Resource          = "b",
            ExpiresOn         = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var uploadUrl = blobClient.GenerateSasUri(sasBuilder).ToString();
        var blobUrl   = blobClient.Uri.ToString();

        _logger.LogInformation("Generated SAS upload URL for blob: {BlobName}", blobName);
        return new SasUploadResult(uploadUrl, blobUrl);
    }

    public async Task DeleteAsync(string fileUrl)
    {
        if (!_isConfigured || _container is null) return;
        var uri  = new Uri(fileUrl);
        var name = Path.GetFileName(uri.LocalPath);
        var blob = _container.GetBlobClient(name);
        await blob.DeleteIfExistsAsync();
    }
}
