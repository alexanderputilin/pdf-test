using System.Text.Json;
using FileStorage;
using Pdf.Api.Configurations;
using Pdf.Models;
using StackExchange.Redis;

namespace Pdf.Api.Services;

public class ApiService
{
    private readonly IFileStorage _fileStorage;
    private readonly IDatabase _redis;
    private readonly CacheNames _cacheNames;

    public ApiService(IConfiguration configuration, IFileStorage fileStorage, IConnectionMultiplexer connection)
    {
        _fileStorage = fileStorage;
        _redis = connection.GetDatabase();
        _cacheNames = configuration.GetSection("CacheNames").Get<CacheNames>() ??
                      throw new Exception("CacheNames configuration not found");
    }

    public async Task DeleteFile(Guid id)
    {
        var fileTaskStr = await _redis.StringGetAsync($"{id}");
        if (string.IsNullOrEmpty(fileTaskStr))
        {
            return;
        }

        var fileTask = JsonSerializer.Deserialize<FileTask>(fileTaskStr.ToString());
        if (fileTask == null)
        {
            return;
        }

        if (fileTask.State == FileTask.EFileState.Done)
        {
            await _fileStorage.Delete($"{id}.pdf");
        }

        await _fileStorage.Delete($"{id}");
        await _redis.KeyDeleteAsync($"{id}");
    }

    public Task<byte[]> GetPdfFile(Guid id)
    {
        return _fileStorage.Get($"{id}.pdf");
    }


    public FileTask? GetFileTask(Guid id)
    {
        var fileTaskStr = _redis.StringGet($"{id}");
        if (string.IsNullOrEmpty(fileTaskStr))
        {
            return null;
        }

        var fileTask = JsonSerializer.Deserialize<FileTask>(fileTaskStr.ToString());
        if (fileTask == null)
        {
            return null;
        }

        return fileTask;
    }

    public async Task<FileTask> SaveFile(string name, byte[] content)
    {
        var fileTask = new FileTask
        {
            Id = Guid.NewGuid(),
            Name = name,
            State = FileTask.EFileState.Queued,
            DoneSet = _cacheNames.DoneSet
        };
        await _fileStorage.Save($"{fileTask.Id}", content);
        await _redis.StringSetAsync($"{fileTask.Id}", JsonSerializer.Serialize(fileTask));
        await _redis.ListRightPushAsync(_cacheNames.QueuedList, $"{fileTask.Id}");
        return fileTask;
    }
}