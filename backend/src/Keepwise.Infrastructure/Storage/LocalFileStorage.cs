using Keepwise.Application.Abstractions;

namespace Keepwise.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var safe = Path.GetFileName(fileName);
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}-{safe}";
        var path = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar));
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
