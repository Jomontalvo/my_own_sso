using System.Diagnostics;

namespace Identity.Sso.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
        var traceId = Activity.Current?.Id ?? string.Empty;
        var sw = Stopwatch.StartNew();

        var requestLog = new
        {
            CorrelationId = correlationId,
            TraceId = traceId,
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            // The query string of an authorization request is never logged: it carries login_hint,
            // state and redirect URIs, and a failed request may echo credentials-adjacent values.
            ClientIp = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.FirstOrDefault(),
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Incoming request {@Request}", requestLog);

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var responseLog = new
            {
                CorrelationId = correlationId,
                TraceId = traceId,
                StatusCode = context.Response.StatusCode,
                DurationMs = sw.ElapsedMilliseconds,
                Timestamp = DateTime.UtcNow
            };

            if (context.Response.StatusCode >= 400)
            {
                _logger.LogWarning("Request completed with error {@Response}", responseLog);
            }
            else
            {
                _logger.LogInformation("Request completed {@Response}", responseLog);
            }
        }
    }
}
