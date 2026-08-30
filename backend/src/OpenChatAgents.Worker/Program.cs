using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Infrastructure.Agents;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Messaging;
using OpenChatAgents.Infrastructure.Options;
using OpenChatAgents.Infrastructure.Storage;
using OpenChatAgents.Infrastructure.Telemetry;
using OpenChatAgents.Infrastructure.VectorStore;
using OpenChatAgents.Worker;
using Weaviate.Client.DependencyInjection;
using Weaviate.Client.VectorData.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
var appOptions = builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>() ?? new AppOptions();

builder.AddOpenChatAgentsTelemetry("OpenChatAgents.Worker", includeAspNetCore: false);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddWeaviate(options =>
{
    options.RestEndpoint = appOptions.Weaviate.RestEndpoint;
    options.RestPort = (ushort)appOptions.Weaviate.RestPort;
    options.GrpcEndpoint = appOptions.Weaviate.GrpcEndpoint;
    options.GrpcPort = (ushort)appOptions.Weaviate.GrpcPort;
    options.UseSsl = appOptions.Weaviate.UseSsl;
});
builder.Services.AddWeaviateVectorStore();

builder.Services.AddSingleton<MinioObjectStore>();
builder.Services.AddSingleton<KbVectorStore>();
builder.Services.AddSingleton<EmbeddingClientFactory>();
builder.Services.AddSingleton<RabbitMqConnectionFactory>();

builder.Services.AddHostedService<KbIngestionBackgroundService>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var objectStore = scope.ServiceProvider.GetRequiredService<MinioObjectStore>();
    await objectStore.EnsureBucketExistsAsync();
}

host.Run();
