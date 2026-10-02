using System.Security.Cryptography;
using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace Api.Tests;

public sealed class LocalFileDocumentStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GalentTests", Guid.NewGuid().ToString("N"));
    private readonly LocalFileDocumentStore _store;

    public LocalFileDocumentStoreTests()
    {
        _store = new LocalFileDocumentStore(Options.Create(new StorageOptions { Root = _root }), new TestHostEnvironment());
    }

    [Fact]
    public async Task SaveAndOpenRead_RoundTripsBytesAndReturnsSha256()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        await using var input = new MemoryStream(bytes);

        var saved = await _store.SaveAsync("user_1", Guid.NewGuid(), input, CancellationToken.None);
        await using var output = await _store.OpenReadAsync(saved.RelativePath, CancellationToken.None);
        using var actual = new MemoryStream();
        await output.CopyToAsync(actual);

        Assert.Equal(bytes, actual.ToArray());
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), saved.Sha256);
        Assert.EndsWith(".pdf", saved.RelativePath, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../outside")]
    [InlineData("C:\\outside")]
    public async Task SaveAsync_RejectsUnsafeUserId(string userId)
    {
        await using var input = new MemoryStream([1]);

        await Assert.ThrowsAsync<ArgumentException>(() => _store.SaveAsync(userId, Guid.NewGuid(), input, CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_RejectsNullStream()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.SaveAsync("valid_user", Guid.NewGuid(), null!, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../outside.pdf")]
    [InlineData("/absolute/path.pdf")]
    public async Task OpenReadAsync_RejectsUnsafeRelativePath(string path)
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _store.OpenReadAsync(path, CancellationToken.None));
    }

    [Fact]
    public async Task OpenReadAsync_ThrowsForMissingFile()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => _store.OpenReadAsync("Development/user/id/missing.pdf", CancellationToken.None));
    }

    [Fact]
    public async Task OpenReadAsync_ThrowsWhenCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _store.OpenReadAsync("Development/user/id/missing.pdf", cancellation.Token));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Api.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
