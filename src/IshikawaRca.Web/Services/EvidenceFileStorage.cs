using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Web.Services;

public class EvidenceStorageOptions
{
    public string RootPath { get; set; } = "App_Data";
}

public record StoredEvidenceFile(
    string FileName,
    string ContentType,
    long SizeBytes,
    string StorageProvider,
    string StorageKey,
    string Sha256);

public record EvidenceFileResolution(
    string PhysicalPath,
    string FileName,
    string ContentType);

public interface IEvidenceFileStorage
{
    long MaxFileSizeBytes { get; }

    Task<StoredEvidenceFile> SaveAsync(Guid incidentId, IFormFile file, CancellationToken cancellationToken);

    EvidenceFileResolution Resolve(string storageKey, string? fileName, string? contentType);

    void Delete(string? storageKey);
}

public class EvidenceFileStorage : IEvidenceFileStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".csv",
        ".txt",
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif",
        ".mp4",
        ".mov",
        ".avi",
        ".mkv",
        ".webm"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly EvidenceStorageOptions _options;

    public EvidenceFileStorage(IWebHostEnvironment environment, IOptions<EvidenceStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public long MaxFileSizeBytes => 100L * 1024L * 1024L;

    public async Task<StoredEvidenceFile> SaveAsync(Guid incidentId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("El archivo de evidencia esta vacio.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("El archivo supera el limite de 100 MB.");
        }

        var originalFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Tipo de archivo no permitido para evidencia RCA.");
        }

        var safeFileName = string.IsNullOrWhiteSpace(originalFileName)
            ? $"evidence{extension}"
            : originalFileName;
        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var relativeDirectory = Path.Combine("rca-evidence", incidentId.ToString("N"));
        var physicalDirectory = Path.Combine(GetStorageRoot(), relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var physicalPath = Path.Combine(physicalDirectory, storedFileName);
        await using (var target = File.Create(physicalPath))
        await using (var source = file.OpenReadStream())
        {
            await source.CopyToAsync(target, cancellationToken);
        }

        var sha256 = await ComputeSha256Async(physicalPath, cancellationToken);
        var storageKey = Path.Combine(relativeDirectory, storedFileName).Replace('\\', '/');
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        return new StoredEvidenceFile(
            safeFileName,
            contentType,
            file.Length,
            "LocalFileSystem",
            storageKey,
            sha256);
    }

    public EvidenceFileResolution Resolve(string storageKey, string? fileName, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(storageKey))
        {
            throw new InvalidOperationException("Storage key de evidencia invalido.");
        }

        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.GetFullPath(Path.Combine(GetStorageRoot(), normalizedKey));
        var root = Path.GetFullPath(GetStorageRoot());
        if (!physicalPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(physicalPath))
        {
            throw new FileNotFoundException("No se encontro el archivo de evidencia.");
        }

        return new EvidenceFileResolution(
            physicalPath,
            string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(physicalPath) : fileName,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
    }

    public void Delete(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(storageKey))
        {
            return;
        }

        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.GetFullPath(Path.Combine(GetStorageRoot(), normalizedKey));
        var root = Path.GetFullPath(GetStorageRoot());
        if (!physicalPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(physicalPath))
        {
            return;
        }

        File.Delete(physicalPath);
    }

    private string GetStorageRoot()
    {
        var configuredRoot = string.IsNullOrWhiteSpace(_options.RootPath)
            ? "App_Data"
            : Environment.ExpandEnvironmentVariables(_options.RootPath);

        return Path.IsPathRooted(configuredRoot)
            ? Path.GetFullPath(configuredRoot)
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredRoot));
    }

    private static async Task<string> ComputeSha256Async(string physicalPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(physicalPath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
