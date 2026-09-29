using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class StorageOptions
{
    public string Root { get; set; } = "storage";
}

public sealed class LocalFileDocumentStore : IDocumentStore
{
    private static readonly Regex SafeSegment = new("^[A-Za-z0-9_-]{1,128}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly string _root;
    private readonly string _rootPrefix;
    private readonly string _environment;

    public LocalFileDocumentStore(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        _root = Path.GetFullPath(options.Value.Root);
        _rootPrefix = Path.EndsInDirectorySeparator(_root) ? _root : _root + Path.DirectorySeparatorChar;
        _environment = SafePathSegment(environment.EnvironmentName);
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredDocument> SaveAsync(string userId, Guid submissionId, Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var safeUserId = SafePathSegment(userId);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", System.Globalization.CultureInfo.InvariantCulture);
        var relativePath = Path.Combine(_environment, safeUserId, submissionId.ToString("N"), $"{timestamp}-{Guid.NewGuid():N}.pdf");
        var fullPath = ResolvePath(relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await content.CopyToAsync(destination, cancellationToken);
                await destination.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, fullPath, overwrite: false);
            await using var storedContent = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(storedContent, cancellationToken));
            return new StoredDocument(Path.GetRelativePath(_root, fullPath), hash);
        }
        catch
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            throw;
        }
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException("The stored document was not found.");
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    private string ResolvePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new UnauthorizedAccessException("Invalid stored document path.");

        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!fullPath.StartsWith(_rootPrefix, comparison))
            throw new UnauthorizedAccessException("Invalid stored document path.");
        return fullPath;
    }

    private static string SafePathSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || !SafeSegment.IsMatch(segment))
            throw new ArgumentException("Identifier cannot be used as a storage path segment.", nameof(segment));
        return segment;
    }
}
