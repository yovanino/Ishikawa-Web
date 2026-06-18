using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Web.Services;

public class ClosureDocumentStorageOptions
{
    public string RootPath { get; set; } = "App_Data";

    public int MaxFileSizeMb { get; set; } = 25;
}

public record StoredClosureDocumentFile(
    string FileName,
    string ContentType,
    long SizeBytes,
    string StorageProvider,
    string StorageKey,
    string Sha256);

public record ClosureDocumentFileResolution(
    string PhysicalPath,
    string FileName,
    string ContentType);

public interface IClosureDocumentStorage
{
    long MaxFileSizeBytes { get; }

    Task<StoredClosureDocumentFile> SaveAsync(Guid incidentId, string fileName, byte[] content, CancellationToken cancellationToken);

    ClosureDocumentFileResolution Resolve(string storageKey, string? fileName, string? contentType);

    void Delete(string? storageKey);
}

public class ClosureDocumentStorage : IClosureDocumentStorage
{
    private readonly IWebHostEnvironment _environment;
    private readonly ClosureDocumentStorageOptions _options;

    public ClosureDocumentStorage(IWebHostEnvironment environment, IOptions<ClosureDocumentStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public long MaxFileSizeBytes => EffectiveMaxFileSizeMb * 1024L * 1024L;

    private int EffectiveMaxFileSizeMb => Math.Max(1, _options.MaxFileSizeMb);

    public async Task<StoredClosureDocumentFile> SaveAsync(Guid incidentId, string fileName, byte[] content, CancellationToken cancellationToken)
    {
        if (content.Length == 0)
        {
            throw new InvalidOperationException("El documento de cierre esta vacio.");
        }

        if (content.LongLength > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"El documento supera el limite de {EffectiveMaxFileSizeMb} MB.");
        }

        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName) ||
            !string.Equals(Path.GetExtension(safeFileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El documento de cierre debe ser un PDF.");
        }

        var storedFileName = $"{Guid.NewGuid():N}.pdf";
        var relativeDirectory = Path.Combine("rca-closure-documents", incidentId.ToString("N"));
        var physicalDirectory = Path.Combine(GetStorageRoot(), relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var physicalPath = Path.Combine(physicalDirectory, storedFileName);
        await File.WriteAllBytesAsync(physicalPath, content, cancellationToken);

        var storageKey = Path.Combine(relativeDirectory, storedFileName).Replace('\\', '/');
        var sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        return new StoredClosureDocumentFile(
            safeFileName,
            "application/pdf",
            content.LongLength,
            "LocalFileSystem",
            storageKey,
            sha256);
    }

    public ClosureDocumentFileResolution Resolve(string storageKey, string? fileName, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(storageKey))
        {
            throw new InvalidOperationException("Storage key de documento invalido.");
        }

        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.GetFullPath(Path.Combine(GetStorageRoot(), normalizedKey));
        var root = Path.GetFullPath(GetStorageRoot());
        if (!IsInsideStorageRoot(root, physicalPath) || !File.Exists(physicalPath))
        {
            throw new FileNotFoundException("No se encontro el documento de cierre.");
        }

        return new ClosureDocumentFileResolution(
            physicalPath,
            string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(physicalPath) : fileName,
            string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType);
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
        if (!IsInsideStorageRoot(root, physicalPath) || !File.Exists(physicalPath))
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

    private static bool IsInsideStorageRoot(string root, string physicalPath)
    {
        var relativePath = Path.GetRelativePath(root, physicalPath);

        return !relativePath.StartsWith("..", StringComparison.Ordinal) &&
               !Path.IsPathRooted(relativePath);
    }
}
