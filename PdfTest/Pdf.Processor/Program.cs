using FileStorage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pdf.Processor.Services;
using StackExchange.Redis;

var environment = Environment.GetEnvironmentVariable("ENVIRONMENT");

var builder = Host.CreateDefaultBuilder().ConfigureHostConfiguration(configHost =>
{
    configHost.SetBasePath(Directory.GetCurrentDirectory());
    configHost.AddJsonFile("appsettings.json", optional: true);
    configHost.AddJsonFile($"appsettings.{environment}.json", optional: true);
    configHost.AddEnvironmentVariables();
    configHost.AddCommandLine(args);
});

builder.ConfigureServices((context, services) =>
{
    services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(
        context.Configuration.GetConnectionString("Redis") ??
        throw new Exception("Redis connection string not found")));
    services.AddHostedService<QueueProcessor>();
    services.AddSingleton<IPdfConvertor, PdfConvertor>();
    services.AddSingleton<IFileStorage, DumbFileStorage.FileStorage>();
});


var app = builder.Build();
app.Run();