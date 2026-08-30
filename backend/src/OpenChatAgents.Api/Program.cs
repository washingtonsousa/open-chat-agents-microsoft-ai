using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenChatAgents.Infrastructure.Agents;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Options;
using OpenChatAgents.Infrastructure.Security;
using OpenChatAgents.Infrastructure.Storage;
using OpenChatAgents.Infrastructure.Telemetry;
using OpenChatAgents.Infrastructure.VectorStore;
using OpenChatAgents.Api.Repositories;
using OpenChatAgents.Api.Services;
using Weaviate.Client.DependencyInjection;
using Weaviate.Client.VectorData.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
var appOptions = builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>() ?? new AppOptions();

builder.AddOpenChatAgentsTelemetry("OpenChatAgents.Api", includeAspNetCore: true);

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
};
builder.Services.AddSingleton(jsonSerializerOptions);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Auth
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = appOptions.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = appOptions.Jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appOptions.Jwt.Secret)),
        };
    });
builder.Services.AddAuthorization();

// Weaviate (vector store)
builder.Services.AddWeaviate(options =>
{
    options.RestEndpoint = appOptions.Weaviate.RestEndpoint;
    options.RestPort = (ushort)appOptions.Weaviate.RestPort;
    options.GrpcEndpoint = appOptions.Weaviate.GrpcEndpoint;
    options.GrpcPort = (ushort)appOptions.Weaviate.GrpcPort;
    options.UseSsl = appOptions.Weaviate.UseSsl;
});
builder.Services.AddWeaviateVectorStore();

// Repositories
builder.Services.AddScoped<AgentRepository>();
builder.Services.AddScoped<SessionRepository>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<KnowledgeBaseRepository>();
builder.Services.AddScoped<KbDocumentRepository>();

// Services
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<KnowledgeBaseService>();
builder.Services.AddScoped<KbRetrievalService>();
builder.Services.AddSingleton<ModerationService>();
builder.Services.AddSingleton<ChatAgentFactory>();
builder.Services.AddSingleton<EmbeddingClientFactory>();
builder.Services.AddSingleton<BedrockModelCatalog>();
builder.Services.AddSingleton<Argon2PasswordHasher>();
builder.Services.AddSingleton<MinioObjectStore>();
builder.Services.AddSingleton<KbVectorStore>();

var corsOrigins = builder.Configuration
    .GetSection($"{AppOptions.SectionName}:CorsOrigins")
    .Get<string[]>() ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    await userService.EnsureDefaultAdminAsync();
}

app.UseCors();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;

        var (statusCode, detail) = exception switch
        {
            ApiException apiEx => ((int)apiEx.StatusCode, apiEx.Detail),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor."),
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { detail }, jsonSerializerOptions);
    });
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();
