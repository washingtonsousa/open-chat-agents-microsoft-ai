using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Api.Agents;
using OpenChatAgents.Api.Data;
using OpenChatAgents.Api.Options;
using OpenChatAgents.Api.Repositories;
using OpenChatAgents.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));

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

builder.Services.AddScoped<AgentRepository>();
builder.Services.AddScoped<SessionRepository>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddSingleton<ModerationService>();
builder.Services.AddSingleton<ChatAgentFactory>();
builder.Services.AddSingleton<BedrockModelCatalog>();

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

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
