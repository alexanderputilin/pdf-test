using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Pdf.Models;
using StackExchange.Redis;

namespace Pdf.Api.Hubs;

public class NotificationHub : Hub
{
    private readonly Dictionary<Guid, string> _fileTaskIdToUserId = new();
    private readonly IConnectionMultiplexer _connection;

    public NotificationHub(IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public Task SendTaskResult(FileTask result)
    {
        if (_fileTaskIdToUserId.TryGetValue(result.Id, out var userId))
        {
            return Clients.Client(userId).SendAsync("TaskResult", result);
        }

        return Task.CompletedTask;
    }

    public async Task SubscribeToFile(Guid id)
    {
        var db = _connection.GetDatabase();
        var fileTaskStr = await db.StringGetAsync(id.ToString());
        if (!string.IsNullOrEmpty(fileTaskStr))
        {
            var fileTask = JsonSerializer.Deserialize<FileTask>(fileTaskStr.ToString());
            if (fileTask?.State == FileTask.EFileState.Done)
            {
                await Clients.Caller.SendAsync("TaskResult", fileTask);
                return;
            }
        }

        _fileTaskIdToUserId[id] = Context.ConnectionId;
        await Clients.All.SendAsync("TaskResult", id);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var subscriptions = _fileTaskIdToUserId.Where(x => x.Value == Context.ConnectionId);
        foreach (var subscription in subscriptions)
        {
            _fileTaskIdToUserId.Remove(subscription.Key);
        }

        return base.OnDisconnectedAsync(exception);
    }
}