using System.Net;

namespace OpenChatAgents.Application.Exceptions;

public class ApiException(HttpStatusCode statusCode, string detail) : Exception(detail)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Detail { get; } = detail;

    public static ApiException NotFound(string detail) => new(HttpStatusCode.NotFound, detail);
    public static ApiException Conflict(string detail) => new(HttpStatusCode.Conflict, detail);
    public static ApiException BadGateway(string detail) => new(HttpStatusCode.BadGateway, detail);
    public static ApiException Unauthorized(string detail) => new(HttpStatusCode.Unauthorized, detail);
    public static ApiException Forbidden(string detail) => new(HttpStatusCode.Forbidden, detail);
}
