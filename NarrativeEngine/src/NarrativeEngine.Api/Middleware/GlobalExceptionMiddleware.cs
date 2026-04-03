using System.Net;
using System.Text.Json;

namespace NarrativeEngine.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode) = exception switch
        {
            KeyNotFoundException        => (HttpStatusCode.NotFound,            "NOT_FOUND"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,        "UNAUTHORIZED"),
            InvalidOperationException   => (HttpStatusCode.BadRequest,          "INVALID_OPERATION"),
            ArgumentException           => (HttpStatusCode.BadRequest,          "BAD_REQUEST"),
            _                           => (HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR")
        };

        var traceId = context.TraceIdentifier;
        var response = new { error = new { code = errorCode, message = exception.Message, traceId } };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
