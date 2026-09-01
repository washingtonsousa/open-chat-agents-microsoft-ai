using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Application.Services;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Options;
using OpenChatAgents.Domain.Repositories;
using OpenChatAgents.Domain.VectorStore;
using OpenChatAgents.Infrastructure.Agents;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Ingestion;
using OpenChatAgents.Infrastructure.Messaging;
using OpenChatAgents.Infrastructure.Persistence;
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

// Repositories (Infrastructure implementa as interfaces definidas no Domain)
builder.Services.AddScoped<IKbDocumentRepository, KbDocumentRepository>();

// Infrastructure (implementações concretas das portas do Domain)
builder.Services.AddSingleton<IObjectStore, MinioObjectStore>();
builder.Services.AddSingleton<IKbVectorStore, KbVectorStore>();
builder.Services.AddSingleton<IEmbeddingClientFactory, EmbeddingClientFactory>();
builder.Services.AddSingleton<ITextExtractor, TextExtractor>();
builder.Services.AddSingleton<RabbitMqConnectionFactory>();

// Application services (casos de uso)
builder.Services.AddScoped<KbIngestionService>();

builder.Services.AddHostedService<KbIngestionBackgroundService>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var objectStore = scope.ServiceProvider.GetRequiredService<IObjectStore>();
    await objectStore.EnsureBucketExistsAsync();
}

host.Run();
