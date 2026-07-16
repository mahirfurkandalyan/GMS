using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Common;

/// <summary>
/// Maps known domain/persistence exceptions to meaningful HTTP responses:
/// invalid status transitions → 400, concurrent updates → 409. Keeps controllers
/// and services free of cross-cutting error translation.
/// </summary>
public sealed class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public DomainExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AuthValidationException ex)
        {
            await Write(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (DocumentValidationException ex)
        {
            await Write(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (IntegrationValidationException ex)
        {
            await Write(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (IntegrationSignatureException ex)
        {
            await Write(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (IntegrationDuplicateException ex)
        {
            await Write(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (DocumentIntegrityException ex)
        {
            await Write(context, StatusCodes.Status500InternalServerError, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await Write(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (AuthFailedException ex)
        {
            await Write(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (AuthForbiddenException ex)
        {
            await Write(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await Write(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (InvalidStatusTransitionException ex)
        {
            await Write(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            await Write(context, StatusCodes.Status409Conflict,
                "Kayıt başka bir işlem tarafından güncellendi. Lütfen sayfayı yenileyip tekrar deneyin.");
        }
    }

    private static async Task Write(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message });
    }
}
