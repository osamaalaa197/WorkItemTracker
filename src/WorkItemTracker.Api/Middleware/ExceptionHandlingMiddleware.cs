using System.Net;
using System.Text.Json;
using WorkItemTracker.Domain.Exceptions;

namespace WorkItemTracker.Api.Middleware;

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
        catch (DomainException ex)
        {
            var status = ex switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                InvalidTransitionException => HttpStatusCode.Conflict,
                DomainValidationException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.BadRequest
            };

            _logger.LogInformation(ex, "Handled domain exception -> {Status}", status);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)status;

            var problem = new
            {
                type = $"https://httpstatuses.com/{(int)status}",
                title = status.ToString(),
                status = (int)status,
                detail = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var problem = new
            {
                type = "https://httpstatuses.com/500",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
