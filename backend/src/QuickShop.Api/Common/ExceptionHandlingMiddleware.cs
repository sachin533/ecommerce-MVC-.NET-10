using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace QuickShop.Api.Common;

/// <summary>
/// Maps thrown exceptions to consistent RFC 7807 problem+json responses so the
/// frontend always receives a predictable error shape.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Title, ex.Message, ex);
        }
        catch (BadHttpRequestException ex)
        {
            await WriteAsync(context, 400, "Bad request", ex.Message, null);
        }
        catch (JsonException)
        {
            await WriteAsync(context, 400, "Invalid JSON", "The request body is not valid JSON.", null);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client went away — nothing to write.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, 500, "Server error", "An unexpected error occurred. Please try again.", null);
        }
    }

    private async Task WriteAsync(HttpContext context, int status, string title, string detail, ApiException? apiEx)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = context.Request.Path
        };

        if (apiEx is ValidationException validation && validation.Errors.Count > 0)
        {
            problem.Extensions["errors"] = validation.Errors;
        }

        if (apiEx is PaymentFailedException payment)
        {
            problem.Extensions["orderNumber"] = payment.OrderNumber;
            if (payment.ProviderReason is not null)
                problem.Extensions["providerReason"] = payment.ProviderReason;
        }

        if (status >= 500 && _environment.IsDevelopment())
            problem.Extensions["stackTrace"] = apiEx?.StackTrace ?? "n/a";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
