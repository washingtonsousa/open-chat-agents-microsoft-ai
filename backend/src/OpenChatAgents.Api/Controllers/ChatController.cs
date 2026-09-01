using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Services;
using OpenChatAgents.Application.Exceptions;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
public class ChatController(ChatService service, JsonSerializerOptions jsonOptions) : ControllerBase
{
    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequest payload, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        try
        {
            await foreach (var evt in service.StreamMessageAsync(payload.SessionId, payload.Message, userId).WithCancellation(cancellationToken))
            {
                await WriteSseAsync(evt.Event, evt.Data, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // cliente desconectou do stream — encerra silenciosamente, igual ao CancelledError do Python
        }
    }

    [HttpGet("{sessionId:guid}/history")]
    public async Task<ActionResult<ChatHistoryResponse>> GetHistory(Guid sessionId)
    {
        var messages = await service.GetHistoryAsync(sessionId);
        return Ok(new ChatHistoryResponse
        {
            SessionId = sessionId,
            Messages = [.. messages.Select(MessageResponse.FromEntity)],
        });
    }

    private async Task WriteSseAsync(string eventName, object data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, jsonOptions);
        var payload = Encoding.UTF8.GetBytes($"event: {eventName}\ndata: {json}\n\n");
        await Response.Body.WriteAsync(payload, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
