using Identity.Sso.Application.Exceptions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Sso.Api.Middleware;

/// <summary>
/// Translates application exceptions into RFC 7807 problem responses, except under <c>/connect</c>,
/// where OAuth 2.0 clients expect the <c>error</c>/<c>error_description</c> shape of RFC 6749 §5.2.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (NotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, "Not Found", Errors.InvalidRequest, ex.Message, null);
        }
        catch (ApplicationValidationException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, "Validation Error", Errors.InvalidRequest, ex.Message, ex.ValidationErrorList);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString();
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path} (CorrelationId: {CorrelationId})",
                context.Request.Method, context.Request.Path, correlationId);

            await WriteAsync(context, StatusCodes.Status500InternalServerError, "Internal Server Error",
                Errors.ServerError, "An unexpected error occurred.", null);
        }
    }

    private static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string oauthError,
        string detail,
        List<string>? errors)
    {
        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        if (context.Request.Path.StartsWithSegments("/connect", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new
            {
                error = oauthError,
                error_description = detail
            });
        }

        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6",
            title,
            status = statusCode,
            detail,
            errors
        });
    }
}
