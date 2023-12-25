using DumbFileStorage.Configurations;
using FileStorage;
using Microsoft.Extensions.Configuration;

namespace DumbFileStorage;

public class FileStorage : IFileStorage
{
    private readonly DumbFileStorageConfiguration _configuration;

    public FileStorage(IConfiguration configuration)
    {
        _configuration = configuration.GetSection("DumbFileStorage").Get<DumbFileStorageConfiguration>() ??
                         throw new Exception("DumbFileStorage configuration not found");
    }

    public Task Save(string name, byte[] content)
    {
        if (!Directory.Exists(_configuration.StoragePath))
        {
            Directory.CreateDirectory(_configuration.StoragePath);
        }

        var path = Path.Combine(_configuration.StoragePath, name);
        return File.WriteAllBytesAsync(path, content);
    }

    public Task<byte[]> Get(string name)
    {
        var path = Path.Combine(_configuration.StoragePath, name);
        return File.ReadAllBytesAsync(path);
    }

    public Task Delete(string name)
    {
        var path = Path.Combine(_configuration.StoragePath, name);
        File.Delete(path);
        return Task.CompletedTask;
    }
}