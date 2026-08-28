using System.Net;
using Keepwise.Application.Common;
using Keepwise.Domain;

namespace Keepwise.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteAsync(context, ex);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception ex)
    {
        var (status, code, message, errors) = ex switch
        {
            AppValidationException validation => (HttpStatusCode.BadRequest, "validation_error", validation.Message, validation.Errors),
            DomainException domain => (HttpStatusCode.BadRequest, domain.Code, domain.Message, null),
            NotFoundException => (HttpStatusCode.NotFound, "not_found", ex.Message, null),
            ForbiddenException => (HttpStatusCode.Forbidden, "forbidden", ex.Message, null),
            ConflictException => (HttpStatusCode.Conflict, "conflict", ex.Message, null),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "unauthorized", ex.Message, null),
            _ => (HttpStatusCode.InternalServerError, "internal_error", "An unexpected error occurred.", null)
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            logger.LogError(ex, "Unhandled exception");
        }

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                code,
                message,
                errors
            }
        });
    }
}
