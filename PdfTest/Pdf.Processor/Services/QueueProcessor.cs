using System.Text;
using System.Text.Json;
using FileStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pdf.Models;
using Pdf.Processor.Configurations;
using StackExchange.Redis;

namespace Pdf.Processor.Services;

public class QueueProcessor : IHostedService
{
    private IPdfConvertor _pdfConvertor;
    private readonly IFileStorage _fileStorage;
    private readonly IDatabase _redis;
    private readonly ILogger<QueueProcessor> _logger;
    private readonly CacheNames _cacheNames;

    public QueueProcessor(IConfiguration configuration, IFileStorage fileStorage, IConnectionMultiplexer connection,
        ILogger<QueueProcessor> logger, IPdfConvertor pdfConvertor)
    {
        _fileStorage = fileStorage;
        _redis = connection.GetDatabase();
        _logger = logger;
        _pdfConvertor = pdfConvertor;
        _cacheNames = configuration.GetSection("CacheNames").Get<CacheNames>() ??
                      throw new Exception("CacheNames configuration not found");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var db = _redis.Multiplexer.GetDatabase();
        for (string? id; (id = db.SetPop(_cacheNames.ProcessingSet)) != null;)
        {
            db.ListRightPush(_cacheNames.QueuedList, id);
        }


        return Task.Run(Process, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }


    private async Task Process()
    {
        var db = _redis.Multiplexer.GetDatabase();
        while (true)
        {
            var id = await db.ListRightPopAsync(_cacheNames.QueuedList);
            if (id.HasValue)
            {
                var fileTaskStr = await db.StringGetAsync(id.ToString());
                if (string.IsNullOrEmpty(fileTaskStr))
                {
                    //unlikely
                    _logger.LogError($"FileTask with id {id} not found");
                    continue;
                }

                var fileTask = JsonSerializer.Deserialize<FileTask>(fileTaskStr.ToString());
                if (fileTask == null)
                {
                    //unlikely
                    _logger.LogError($"FileTask with id {id} deserialization failed");
                    continue;
                }

                await db.SetAddAsync(_cacheNames.ProcessingSet, id.ToString());
                try
                {
                    var content = Encoding.UTF8.GetString(await _fileStorage.Get(id.ToString()));
                    var result = await _pdfConvertor.GetPdf(content);

                    await _fileStorage.Save($"{id}.pdf", result);
                    fileTask.State = FileTask.EFileState.Done;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error processing file {id}");
                    fileTask.State = FileTask.EFileState.Error;
                }

                await db.StringSetAsync(id.ToString(), JsonSerializer.Serialize(fileTask));
                await db.SetRemoveAsync(_cacheNames.ProcessingSet, id.ToString());
                await db.SetAddAsync(fileTask.DoneSet, id.ToString());
            }
            else
            {
                await Task.Delay(500);
            }
        }
    }
}