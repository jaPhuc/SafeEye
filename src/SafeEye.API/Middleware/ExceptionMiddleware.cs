using System.Net;
using System.Text.Json;
using SafeEye.Domain.Exceptions;
using FluentValidation;

namespace SafeEye.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await HandleAsync(ctx, ex);
        }
    }

    private static Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, message, errors) = ex switch
        {
            ValidationException ve => (
                HttpStatusCode.UnprocessableEntity, "Validation failed.",
                ve.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })),
            NotFoundException => (HttpStatusCode.NotFound, ex.Message, Enumerable.Empty<object>()),
            ConflictException => (HttpStatusCode.Conflict, ex.Message, Enumerable.Empty<object>()),
            ForbiddenException => (HttpStatusCode.Forbidden, ex.Message, Enumerable.Empty<object>()),
            UnauthorizedException => (HttpStatusCode.Unauthorized, ex.Message, Enumerable.Empty<object>()),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", Enumerable.Empty<object>()),
        };

        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json";

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(
            new { error = message, errors = errors.Any() ? errors : null },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}