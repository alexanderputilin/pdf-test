namespace Pdf.Api.Test;

using Xunit;
using Moq;
using System;
using Services;
using Models;
using FileStorage;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

public class ApiServiceTests
{
    private readonly Mock<IFileStorage> _mockFileStorage;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly ApiService _apiService;

    public ApiServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"CacheNames:QueuedList", "queued-list"},
            {"CacheNames:DoneSet", "done-set"},
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _mockFileStorage = new Mock<IFileStorage>();
        Mock<IConnectionMultiplexer> mockConnection = new();
        _mockDatabase = new Mock<IDatabase>();
        mockConnection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);

        _apiService = new ApiService(configuration, _mockFileStorage.Object, mockConnection.Object);
    }


    [Fact]
    public async Task SaveFile_ShouldSaveFile_WhenCalled()
    {
        // Arrange
        var name = "testFile";
        var content = new byte[] {0x01, 0x02};
        var expectedFileTask = new FileTask
        {
            Id = It.IsAny<Guid>(),
            Name = name,
            State = FileTask.EFileState.Queued,
            DoneSet = "done-set"
        };
        _mockFileStorage.Setup(f => f.Save(It.IsAny<string>(), content)).Returns(Task.CompletedTask);
        _mockDatabase
            .Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, When.Always,
                CommandFlags.None)).ReturnsAsync(true);
        _mockDatabase.Setup(d =>
                d.ListRightPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), When.Always, CommandFlags.None))
            .ReturnsAsync(1);

        // Act
        var actualFileTask = await _apiService.SaveFile(name, content);

        // Assert
        Assert.Equal(expectedFileTask.Name, actualFileTask.Name);
        Assert.Equal(expectedFileTask.State, actualFileTask.State);
        _mockFileStorage.Verify(f => f.Save(It.IsAny<string>(), content), Times.Once);
    }

    [Fact]
    public async Task DeleteFile_ShouldDeleteFile_WhenFileExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var fileTask = new FileTask
        {
            Id = id,
            State = FileTask.EFileState.Done
        };
        var serializedFileTask = JsonSerializer.Serialize(fileTask);
        _mockDatabase.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedFileTask);

        // Act
        await _apiService.DeleteFile(id);

        // Assert
        _mockFileStorage.Verify(f => f.Delete(It.IsAny<string>()), Times.Exactly(2));
        _mockDatabase.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public void GetFileTask_ShouldReturnFileTask_WhenFileTaskExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedFileTask = new FileTask
        {
            Id = id,
            State = FileTask.EFileState.Done
        };
        var serializedFileTask = JsonSerializer.Serialize(expectedFileTask);
        _mockDatabase.Setup(d => d.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(serializedFileTask);

        // Act
        var actualFileTask = _apiService.GetFileTask(id);

        // Assert
        Assert.Equal(expectedFileTask.Id, actualFileTask?.Id);
        Assert.Equal(expectedFileTask.State, actualFileTask?.State);
        _mockDatabase.Verify(d => d.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}