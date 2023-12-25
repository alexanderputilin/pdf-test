using FileStorage;
using Pdf.Api.Hubs;
using Pdf.Api.Services;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHostedService<ResultProcessor>();
builder.Services.AddSingleton<NotificationHub>();
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis") ??
    throw new Exception("Redis connection string not found")));
builder.Services.AddScoped<ApiService>();
builder.Services.AddSingleton<IFileStorage, DumbFileStorage.FileStorage>();
var app = builder.Build();

app.MapControllers();
app.MapHub<NotificationHub>("/api/notifications");
app.Run();