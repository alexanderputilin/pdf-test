using System.Text.Json;
using Pdf.Api.Configurations;
using Pdf.Api.Hubs;
using Pdf.Models;
using StackExchange.Redis;

namespace Pdf.Api.Services;

public class ResultProcessor : BackgroundService
{
    private readonly NotificationHub _notificationHub;
    private readonly IDatabase _redis;
    private readonly CacheNames _cacheNames;

    public ResultProcessor(IConfiguration configuration, NotificationHub notificationHub,
        IConnectionMultiplexer connection)
    {
        _notificationHub = notificationHub;
        _redis = connection.GetDatabase();
        _cacheNames = configuration.GetSection("CacheNames").Get<CacheNames>() ??
                      throw new Exception("CacheNames configuration not found");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var id = await _redis.SetPopAsync(_cacheNames.DoneSet);
            if (id.HasValue)
            {
                var fileTaskStr = await _redis.StringGetAsync(id.ToString());
                if (string.IsNullOrEmpty(fileTaskStr))
                {
                    continue;
                }

                var fileTask = JsonSerializer.Deserialize<FileTask>(fileTaskStr.ToString());
                if (fileTask == null)
                {
                    continue;
                }

                await _notificationHub.SendTaskResult(fileTask);
            }
            else
            {
                await Task.Delay(500, stoppingToken);
            }
        }
    }
}