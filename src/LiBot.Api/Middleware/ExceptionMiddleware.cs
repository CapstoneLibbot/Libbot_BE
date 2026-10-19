namespace LiBot.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex)
        {
            if (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogDebug(
                    "Client disconnected while sending {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);
                return;
            }

            _logger.LogWarning(
                "Rejected incomplete or invalid HTTP request {Method} {Path} with status {StatusCode}.",
                context.Request.Method,
                context.Request.Path,
                ex.StatusCode);

            if (context.Response.HasStarted) return;

            context.Response.Clear();
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "The request was incomplete or invalid.",
                path = context.Request.Path
            });
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Client disconnected while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception ex)
        {
            if (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogDebug(
                    "Request {Method} {Path} was aborted by the client.",
                    context.Request.Method,
                    context.Request.Path);
                return;
            }

            _logger.LogError(ex, "Unhandled exception while processing request {Path}", context.Request.Path);

            if (context.Response.HasStarted) return;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred.",
                path = context.Request.Path
            });
        }
    }
}
